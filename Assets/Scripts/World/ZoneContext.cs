using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeyondTheBeat.World
{
    public enum WorldZoneType
    {
        Urban = 0,
        OffRoad = 1,
        Forest = 2
    }

    [RequireComponent(typeof(Collider))]
    public sealed class ZoneContext : MonoBehaviour
    {
        [SerializeField] private string zoneId = "zone";
        [SerializeField] private WorldZoneType zoneType = WorldZoneType.Urban;

        private readonly Dictionary<GameObject, int> overlapCounts = new Dictionary<GameObject, int>(4);

        public string ZoneId => zoneId;
        public WorldZoneType ZoneType => zoneType;
        public int ActiveActorCount => overlapCounts.Count;

        public event Action<ZoneContext, GameObject> ActorEntered;
        public event Action<ZoneContext, GameObject> ActorExited;

        private void Awake()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            GameObject actor = ResolveActor(other);
            if (actor == null)
            {
                return;
            }

            overlapCounts.TryGetValue(actor, out int count);
            overlapCounts[actor] = count + 1;

            if (count == 0)
            {
                ActorEntered?.Invoke(this, actor);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            GameObject actor = ResolveActor(other);
            if (actor == null || !overlapCounts.TryGetValue(actor, out int count))
            {
                return;
            }

            count--;
            if (count <= 0)
            {
                overlapCounts.Remove(actor);
                ActorExited?.Invoke(this, actor);
            }
            else
            {
                overlapCounts[actor] = count;
            }
        }

        private void OnDisable()
        {
            if (overlapCounts.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<GameObject, int> entry in overlapCounts)
            {
                if (entry.Key != null)
                {
                    ActorExited?.Invoke(this, entry.Key);
                }
            }

            overlapCounts.Clear();
        }

        public bool IsActorInside(GameObject actor)
        {
            return actor != null && overlapCounts.ContainsKey(actor);
        }

        private static GameObject ResolveActor(Collider other)
        {
            if (other == null)
            {
                return null;
            }

            Rigidbody attachedBody = other.attachedRigidbody;
            if (attachedBody != null)
            {
                return attachedBody.gameObject;
            }

            Transform root = other.transform.root;
            return root != null ? root.gameObject : other.gameObject;
        }
    }
}
