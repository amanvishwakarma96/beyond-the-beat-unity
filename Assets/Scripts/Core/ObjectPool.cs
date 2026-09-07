using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeyondTheBeat.Core
{
    /// <summary>
    /// Small allocation-conscious component pool for future repeated effects/hazards.
    /// Example: var pool = new ObjectPool<MyFx>(() => Instantiate(prefab), 8, transform);
    /// MyFx fx = pool.Get(); ... pool.Release(fx);
    /// </summary>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly Func<T> factory;
        private readonly Stack<T> available;
        private readonly HashSet<T> leased;
        private readonly Transform inactiveParent;

        public int AvailableCount => available.Count;
        public int LeasedCount => leased.Count;
        public int TotalCount => AvailableCount + LeasedCount;

        public ObjectPool(Func<T> factory, int prewarmCount = 0, Transform inactiveParent = null)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.inactiveParent = inactiveParent;
            int capacity = Mathf.Max(0, prewarmCount);
            available = new Stack<T>(capacity);
            leased = new HashSet<T>();

            for (int i = 0; i < capacity; i++)
            {
                T item = CreateItem();
                Deactivate(item);
                available.Push(item);
            }
        }

        public T Get()
        {
            T item = available.Count > 0 ? available.Pop() : CreateItem();
            leased.Add(item);
            item.gameObject.SetActive(true);
            return item;
        }

        public bool Release(T item)
        {
            if (item == null || !leased.Remove(item))
            {
                return false;
            }

            Deactivate(item);
            available.Push(item);
            return true;
        }

        public void Clear(bool destroyObjects = true)
        {
            if (destroyObjects)
            {
                foreach (T item in available)
                {
                    DestroySafe(item);
                }

                foreach (T item in leased)
                {
                    DestroySafe(item);
                }
            }

            available.Clear();
            leased.Clear();
        }

        private T CreateItem()
        {
            T item = factory();
            if (item == null)
            {
                throw new InvalidOperationException("ObjectPool factory returned null.");
            }

            return item;
        }

        private void Deactivate(T item)
        {
            if (inactiveParent != null)
            {
                item.transform.SetParent(inactiveParent, false);
            }
            item.gameObject.SetActive(false);
        }

        private static void DestroySafe(T item)
        {
            if (item == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(item.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
        }
    }
}
