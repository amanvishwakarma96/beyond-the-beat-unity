using System;
using BeyondTheBeat.Missions;
using UnityEngine;

namespace BeyondTheBeat.Persistence
{
    [Serializable]
    public struct SerializableVector3
    {
        public float X;
        public float Y;
        public float Z;

        public SerializableVector3(Vector3 value)
        {
            X = value.x;
            Y = value.y;
            Z = value.z;
        }

        public Vector3 ToVector3() => new Vector3(X, Y, Z);
    }

    [Serializable]
    public struct SerializableQuaternion
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public SerializableQuaternion(Quaternion value)
        {
            X = value.x;
            Y = value.y;
            Z = value.z;
            W = value.w;
        }

        public Quaternion ToQuaternion() => new Quaternion(X, Y, Z, W);
    }

    [Serializable]
    public struct SavedTransform
    {
        public SerializableVector3 Position;
        public SerializableQuaternion Rotation;

        public SavedTransform(Vector3 position, Quaternion rotation)
        {
            Position = new SerializableVector3(position);
            Rotation = new SerializableQuaternion(rotation);
        }

        public static SavedTransform Capture(Transform target)
        {
            return target == null
                ? new SavedTransform(Vector3.zero, Quaternion.identity)
                : new SavedTransform(target.position, target.rotation);
        }
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public int Version = SaveManager.CurrentVersion;
        public string SceneId = string.Empty;
        public SavedTransform VehicleTransform = new SavedTransform(Vector3.zero, Quaternion.identity);
        public string MissionId = string.Empty;
        public MissionState MissionState = MissionState.Inactive;
    }
}
