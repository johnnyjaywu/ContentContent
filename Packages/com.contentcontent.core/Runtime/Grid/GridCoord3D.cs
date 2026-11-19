using System;

namespace ContentContent.Core.Grid
{
    /// <summary>
    /// Immutable 3D integer grid coordinate.
    /// </summary>
    public readonly struct GridCoord3D : IEquatable<GridCoord3D>
    {
        public static readonly GridCoord3D Zero = new GridCoord3D(0, 0, 0);
        public static readonly GridCoord3D Right = new GridCoord3D(1, 0, 0);
        public static readonly GridCoord3D Left = new GridCoord3D(-1, 0, 0);
        public static readonly GridCoord3D Up = new GridCoord3D(0, 1, 0);
        public static readonly GridCoord3D Down = new GridCoord3D(0, -1, 0);
        public static readonly GridCoord3D Forward = new GridCoord3D(0, 0, 1);
        public static readonly GridCoord3D Back = new GridCoord3D(0, 0, -1);

        /// <summary>6-way orthogonal directions.</summary>
        public static readonly GridCoord3D[] CardinalDirections =
        {
            Right, Left, Up, Down, Forward, Back
        };

        /// <summary>26-way orthogonal and diagonal directions.</summary>
        public static readonly GridCoord3D[] AllDirections = BuildAllDirections();

        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public GridCoord3D(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int SqrMagnitude => X * X + Y * Y + Z * Z;

        public static int ManhattanDistance(GridCoord3D a, GridCoord3D b)
            => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);

        public static int ChebyshevDistance(GridCoord3D a, GridCoord3D b)
            => Math.Max(Math.Abs(a.X - b.X), Math.Max(Math.Abs(a.Y - b.Y), Math.Abs(a.Z - b.Z)));

        public static GridCoord3D operator +(GridCoord3D a, GridCoord3D b) => new GridCoord3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static GridCoord3D operator -(GridCoord3D a, GridCoord3D b) => new GridCoord3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static GridCoord3D operator *(GridCoord3D a, int scalar) => new GridCoord3D(a.X * scalar, a.Y * scalar, a.Z * scalar);
        public static bool operator ==(GridCoord3D a, GridCoord3D b) => a.Equals(b);
        public static bool operator !=(GridCoord3D a, GridCoord3D b) => !a.Equals(b);

        public bool Equals(GridCoord3D other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is GridCoord3D other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X}, {Y}, {Z})";

        private static GridCoord3D[] BuildAllDirections()
        {
            var dirs = new GridCoord3D[26];
            int idx = 0;
            for (int z = -1; z <= 1; z++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        if (x == 0 && y == 0 && z == 0)
                            continue;
                        dirs[idx++] = new GridCoord3D(x, y, z);
                    }
                }
            }
            return dirs;
        }
    }
}
