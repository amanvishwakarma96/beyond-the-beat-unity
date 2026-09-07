using System;
using System.Collections.Generic;
using BeyondTheBeat.Vehicle;
using UnityEngine;

namespace BeyondTheBeat.World
{
    /// <summary>
    /// Bridges world context into the vehicle condition system without teaching VehicleController
    /// about scene names or zone IDs. Off-road and forest contexts count as high-wear surfaces.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleWearZoneAdapter : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private ZoneContext[] zones = Array.Empty<ZoneContext>();

        private readonly HashSet<ZoneContext> activeWearZones = new HashSet<ZoneContext>();

        public VehicleController VehicleController => vehicleController;
        public int ZoneCount => zones != null ? zones.Length : 0;
        public bool IsHighWearSurfaceActive => activeWearZones.Count > 0;

        private void Awake()
        {
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }
        }

        private void OnEnable()
        {
            BindZones();
        }

        private void OnDisable()
        {
            UnbindZones();
            activeWearZones.Clear();
            vehicleController?.SetOffRoadState(false);
        }

        public void Configure(VehicleController controller, ZoneContext[] contexts)
        {
            bool wasEnabled = isActiveAndEnabled;
            if (wasEnabled)
            {
                UnbindZones();
            }

            vehicleController = controller;
            zones = contexts ?? Array.Empty<ZoneContext>();
            activeWearZones.Clear();

            if (wasEnabled)
            {
                BindZones();
            }
        }

        private void BindZones()
        {
            if (vehicleController == null || zones == null)
            {
                return;
            }

            activeWearZones.Clear();
            for (int i = 0; i < zones.Length; i++)
            {
                ZoneContext zone = zones[i];
                if (zone == null || !IsWearZone(zone))
                {
                    continue;
                }

                zone.ActorEntered -= HandleActorEntered;
                zone.ActorEntered += HandleActorEntered;
                zone.ActorExited -= HandleActorExited;
                zone.ActorExited += HandleActorExited;

                if (zone.IsActorInside(vehicleController.gameObject))
                {
                    activeWearZones.Add(zone);
                }
            }

            RefreshVehicleSurfaceState();
        }

        private void UnbindZones()
        {
            if (zones == null)
            {
                return;
            }

            for (int i = 0; i < zones.Length; i++)
            {
                ZoneContext zone = zones[i];
                if (zone == null)
                {
                    continue;
                }

                zone.ActorEntered -= HandleActorEntered;
                zone.ActorExited -= HandleActorExited;
            }
        }

        private void HandleActorEntered(ZoneContext zone, GameObject actor)
        {
            if (vehicleController == null || actor != vehicleController.gameObject || !IsWearZone(zone))
            {
                return;
            }

            activeWearZones.Add(zone);
            RefreshVehicleSurfaceState();
        }

        private void HandleActorExited(ZoneContext zone, GameObject actor)
        {
            if (vehicleController == null || actor != vehicleController.gameObject || zone == null)
            {
                return;
            }

            activeWearZones.Remove(zone);
            RefreshVehicleSurfaceState();
        }

        private void RefreshVehicleSurfaceState()
        {
            vehicleController?.SetOffRoadState(activeWearZones.Count > 0);
        }

        private static bool IsWearZone(ZoneContext zone)
        {
            return zone != null &&
                   (zone.ZoneType == WorldZoneType.OffRoad || zone.ZoneType == WorldZoneType.Forest);
        }
    }
}
