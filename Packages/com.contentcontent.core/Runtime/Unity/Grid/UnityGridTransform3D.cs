using System;
using UnityEngine;

namespace ContentContent.Core.Grid.Unity
{
    /// <summary>
    /// Converts between 3D <see cref="GridCoord3D"/> and Unity world-space <see cref="Vector3"/> coordinates.
    /// </summary>
    [Serializable]
    public struct UnityGridTransform3D
    {
        public Vector3 origin;
        public float cellSize;

        public UnityGridTransform3D(Vector3 origin, float cellSize = 1f)
        {
            this.origin = origin;
            this.cellSize = Mathf.Max(0.0001f, cellSize);
        }

        /// <summary>Returns the world-space minimum corner of the specified cell.</summary>
        public Vector3 GridToWorldCorner(GridCoord3D coord)
        {
            float size = Mathf.Max(0.0001f, cellSize);
            return origin + new Vector3(coord.X * size, coord.Y * size, coord.Z * size);
        }

        /// <summary>Returns the world-space center of the specified cell.</summary>
        public Vector3 GridToWorldCenter(GridCoord3D coord)
        {
            float size = Mathf.Max(0.0001f, cellSize);
            float half = size * 0.5f;
            return origin + new Vector3(coord.X * size + half, coord.Y * size + half, coord.Z * size + half);
        }

        /// <summary>Converts a world-space position into the containing <see cref="GridCoord3D"/>.</summary>
        public GridCoord3D WorldToGrid(Vector3 worldPosition)
        {
            float size = Mathf.Max(0.0001f, cellSize);
            Vector3 local = worldPosition - origin;
            return new GridCoord3D(
                Mathf.FloorToInt(local.x / size),
                Mathf.FloorToInt(local.y / size),
                Mathf.FloorToInt(local.z / size));
        }

        /// <summary>Converts a Unity <see cref="Vector3Int"/> to a <see cref="GridCoord3D"/>.</summary>
        public static GridCoord3D ToGridCoord3D(Vector3Int v) => new GridCoord3D(v.x, v.y, v.z);

        /// <summary>Converts a <see cref="GridCoord3D"/> to a Unity <see cref="Vector3Int"/>.</summary>
        public static Vector3Int ToVector3Int(GridCoord3D c) => new Vector3Int(c.X, c.Y, c.Z);
    }
}
