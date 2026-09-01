using BeyondTheBeat.World;
using UnityEngine;

namespace BeyondTheBeat.Water
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(ZoneContext))]
    public sealed class WaterVolume : MonoBehaviour
    {
        [Header("Water Context")]
        [SerializeField] private ZoneContext zoneContext;
        [SerializeField] private BoxCollider volumeCollider;

        [Header("Water Data")]
        [SerializeField] private float surfaceY;
        [SerializeField, Min(0.1f)] private float maxDepth = 10f;

        public ZoneContext ZoneContext => zoneContext;
        public BoxCollider VolumeCollider => volumeCollider;
        public float SurfaceY => surfaceY;
        public float MaxDepth => Mathf.Max(0.1f, maxDepth);
        public bool IsConfigured => zoneContext != null && volumeCollider != null;

        private void Awake()
        {
            ResolveReferences();
            if (volumeCollider != null)
            {
                volumeCollider.isTrigger = true;
            }
        }

        private void OnValidate()
        {
            maxDepth = Mathf.Max(0.1f, maxDepth);
            ResolveReferences();
            if (volumeCollider != null)
            {
                volumeCollider.isTrigger = true;
            }
        }

        public bool ContainsPoint(Vector3 worldPoint)
        {
            return volumeCollider != null && volumeCollider.bounds.Contains(worldPoint);
        }

        public bool ContainsHorizontalPosition(Vector3 worldPoint)
        {
            if (volumeCollider == null)
            {
                return false;
            }

            Bounds bounds = volumeCollider.bounds;
            return worldPoint.x >= bounds.min.x &&
                   worldPoint.x <= bounds.max.x &&
                   worldPoint.z >= bounds.min.z &&
                   worldPoint.z <= bounds.max.z;
        }

        public float GetDepthAt(Vector3 worldPoint)
        {
            if (!ContainsHorizontalPosition(worldPoint) || worldPoint.y >= surfaceY)
            {
                return 0f;
            }

            return Mathf.Clamp(surfaceY - worldPoint.y, 0f, MaxDepth);
        }

        public float GetNormalizedDepthAt(Vector3 worldPoint)
        {
            return Mathf.Clamp01(GetDepthAt(worldPoint) / MaxDepth);
        }

        private void ResolveReferences()
        {
            if (zoneContext == null)
            {
                zoneContext = GetComponent<ZoneContext>();
            }

            if (volumeCollider == null)
            {
                volumeCollider = GetComponent<BoxCollider>();
            }
        }
    }
}
