using System;
using System.Collections.Generic;

namespace ContentContent.Core.Grid
{
    /// <summary>
    /// Flat-array backed 3D grid for cache-friendly random access and zero-allocation neighbor queries.
    /// </summary>
    public class Grid3D<TCell> : IGrid3D<TCell>
    {
        private readonly TCell[] _cells;

        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public int Count => _cells.Length;

        public event Action<GridCoord3D, TCell, TCell> OnCellChanged;

        public Grid3D(int width, int height, int depth, Func<GridCoord3D, TCell> initialFactory = null)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");
            if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be greater than 0.");

            Width = width;
            Height = height;
            Depth = depth;
            _cells = new TCell[width * height * depth];

            if (initialFactory != null)
            {
                Fill(initialFactory, notify: false);
            }
        }

        public TCell this[int x, int y, int z]
        {
            get
            {
                ValidateBounds(x, y, z);
                return _cells[ToIndex(x, y, z)];
            }
            set
            {
                ValidateBounds(x, y, z);
                SetUnchecked(x, y, z, value, notify: true);
            }
        }

        public TCell this[GridCoord3D coord]
        {
            get => this[coord.X, coord.Y, coord.Z];
            set => this[coord.X, coord.Y, coord.Z] = value;
        }

        public bool IsInBounds(int x, int y, int z)
            => x >= 0 && x < Width && y >= 0 && y < Height && z >= 0 && z < Depth;

        public bool IsInBounds(GridCoord3D coord) => IsInBounds(coord.X, coord.Y, coord.Z);

        public bool TryGet(GridCoord3D coord, out TCell value)
        {
            if (!IsInBounds(coord))
            {
                value = default;
                return false;
            }

            value = _cells[ToIndex(coord.X, coord.Y, coord.Z)];
            return true;
        }

        public bool TrySet(GridCoord3D coord, TCell value)
        {
            if (!IsInBounds(coord))
                return false;

            SetUnchecked(coord.X, coord.Y, coord.Z, value, notify: true);
            return true;
        }

        public void Fill(TCell value, bool notify = true)
        {
            for (int z = 0; z < Depth; z++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        SetUnchecked(x, y, z, value, notify);
                    }
                }
            }
        }

        public void Fill(Func<GridCoord3D, TCell> factory, bool notify = true)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            for (int z = 0; z < Depth; z++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        SetUnchecked(x, y, z, factory(new GridCoord3D(x, y, z)), notify);
                    }
                }
            }
        }

        public void GetNeighbors(GridCoord3D coord, List<GridCoord3D> results, bool includeDiagonals = false)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();

            GridCoord3D[] directions = includeDiagonals
                ? GridCoord3D.AllDirections
                : GridCoord3D.CardinalDirections;

            for (int i = 0; i < directions.Length; i++)
            {
                GridCoord3D next = coord + directions[i];
                if (IsInBounds(next))
                {
                    results.Add(next);
                }
            }
        }

        public int ToIndex(int x, int y, int z) => x + Width * (y + Height * z);
        public int ToIndex(GridCoord3D coord) => ToIndex(coord.X, coord.Y, coord.Z);

        public GridCoord3D FromIndex(int index)
        {
            int x = index % Width;
            int rem = index / Width;
            int y = rem % Height;
            int z = rem / Height;
            return new GridCoord3D(x, y, z);
        }

        private void SetUnchecked(int x, int y, int z, TCell value, bool notify)
        {
            int index = ToIndex(x, y, z);
            TCell oldValue = _cells[index];
            _cells[index] = value;

            if (notify && OnCellChanged != null && !EqualityComparer<TCell>.Default.Equals(oldValue, value))
            {
                OnCellChanged.Invoke(new GridCoord3D(x, y, z), oldValue, value);
            }
        }

        private void ValidateBounds(int x, int y, int z)
        {
            if (!IsInBounds(x, y, z))
            {
                throw new ArgumentOutOfRangeException(
                    $"Coordinate ({x}, {y}, {z}) is outside grid bounds ({Width}x{Height}x{Depth}).");
            }
        }
    }
}
