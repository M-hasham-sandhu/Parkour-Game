using UnityEngine;

namespace ParkourController
{
    public enum ObstacleType { None, Vault, Mantle }

    public struct ObstacleInfo
    {
        public bool detected;
        public ObstacleType type;
        public float height;
        public Vector3 topPoint;
        public Vector3 hitPoint;

        public static ObstacleInfo None => new ObstacleInfo { detected = false, type = ObstacleType.None };
    }
}