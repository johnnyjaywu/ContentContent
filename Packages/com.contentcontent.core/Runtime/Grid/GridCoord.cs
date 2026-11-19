using System;

namespace ContentContent.Core.Grid
{
    /// <summary>
    /// Immutable 2D integer grid coordinate.
    /// </summary>
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public static readonly GridCoord Zero = new GridCoord(0, 0);
        public static readonly GridCoord Up = new GridCoord(0, 1);
        public static readonly GridCoord Down = new GridCoord(0, -1);
        public static readonly GridCoord Left = new GridCoord(-1, 0);
        public static readonly GridCoord Right = new GridCoord(1, 0);

        public static readonly GridCoord[] CardinalDirections =
        {
            Up, Right, Down, Left
        };

        public static readonly GridCoord[] AllDirections =
        {
            Up, new GridCoord(1, 1), Right, new GridCoord(1, -1),
            Down, new GridCoord(-1, -1), Left, new GridCoord(-1, 1)
        };

        public int X { get; }
        public int Y { get; }

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int SqrMagnitude => X * X + Y * Y;

        public static int ManhattanDistance(GridCoord a, GridCoord b)
            => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        public static int ChebyshevDistance(GridCoord a, GridCoord b)
            => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        public static GridCoord operator +(GridCoord a, GridCoord b) => new GridCoord(a.X + b.X, a.Y + b.Y);
        public static GridCoord operator -(GridCoord a, GridCoord b) => new GridCoord(a.X - b.X, a.Y - b.Y);
        public static GridCoord operator *(GridCoord a, int scalar) => new GridCoord(a.X * scalar, a.Y * scalar);
        public static bool operator ==(GridCoord a, GridCoord b) => a.Equals(b);
        public static bool operator !=(GridCoord a, GridCoord b) => !a.Equals(b);

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";
    }
}
