using System;
using UnityEngine;

namespace ContentContent.Grid.Unity
{
    /// <summary>
    /// Orientation plane used when mapping 2D <see cref="GridCoord"/>s into Unity 3D world space.
    /// </summary>
    public enum GridPlane
    {
        /// <summary>2D games / vertical grids (X = right, Y = up).</summary>
        XY,

        /// <summary>3D top-down / strategy ground grids (X = right, Z = forward).</summary>
        XZ
    }

    /// <summary>
    /// Converts between <see cref="GridCoord"/> and Unity world-space <see cref="Vector3"/> coordinates.
    /// </summary>
    [Serializable]
    public struct UnityGridTransform
    {
        public Vector3 origin;
        public float cellSize;
        public GridPlane plane;

        public UnityGridTransform(Vector3 origin, float cellSize = 1f, GridPlane plane = GridPlane.XZ)
        {
            this.origin = origin;
            this.cellSize = Mathf.Max(0.0001f, cellSize);
            this.plane = plane;
        }

        /// <summary>Returns the world-space bottom-left corner of the specified cell.</summary>
        public Vector3 GridToWorldCorner(GridCoord coord)
        {
            float size = Mathf.Max(0.0001f, cellSize);
            return plane == GridPlane.XY
                ? origin + new Vector3(coord.X * size, coord.Y * size, 0f)
                : origin + new Vector3(coord.X * size, 0f, coord.Y * size);
        }

        /// <summary>Returns the world-space center of the specified cell.</summary>
        public Vector3 GridToWorldCenter(GridCoord coord)
        {
            float half = Mathf.Max(0.0001f, cellSize) * 0.5f;
            Vector3 corner = GridToWorldCorner(coord);
            return plane == GridPlane.XY
                ? corner + new Vector3(half, half, 0f)
                : corner + new Vector3(half, 0f, half);
        }

        /// <summary>Converts a world-space position into the containing <see cref="GridCoord"/>.</summary>
        public GridCoord WorldToGrid(Vector3 worldPosition)
        {
            float size = Mathf.Max(0.0001f, cellSize);
            Vector3 local = worldPosition - origin;
            int x = Mathf.FloorToInt(local.x / size);
            int y = plane == GridPlane.XY
                ? Mathf.FloorToInt(local.y / size)
                : Mathf.FloorToInt(local.z / size);
            return new GridCoord(x, y);
        }

        /// <summary>Converts a Unity <see cref="Vector2Int"/> to a <see cref="GridCoord"/>.</summary>
        public static GridCoord ToGridCoord(Vector2Int v) => new GridCoord(v.x, v.y);

        /// <summary>Converts a <see cref="GridCoord"/> to a Unity <see cref="Vector2Int"/>.</summary>
        public static Vector2Int ToVector2Int(GridCoord c) => new Vector2Int(c.X, c.Y);
    }
}
