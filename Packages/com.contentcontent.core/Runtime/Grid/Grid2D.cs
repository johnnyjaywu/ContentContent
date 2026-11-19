using System;
using System.Collections.Generic;

namespace ContentContent.Core.Grid
{
    /// <summary>
    /// Flat-array backed 2D grid for cache-friendly random access and zero-allocation neighbor queries.
    /// </summary>
    public class Grid2D<TCell> : IGrid<TCell>
    {
        private readonly TCell[] _cells;

        public int Width { get; }
        public int Height { get; }
        public int Count => _cells.Length;

        public event Action<GridCoord, TCell, TCell> OnCellChanged;

        public Grid2D(int width, int height, Func<GridCoord, TCell> initialFactory = null)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");

            Width = width;
            Height = height;
            _cells = new TCell[width * height];

            if (initialFactory != null)
            {
                Fill(initialFactory, notify: false);
            }
        }

        public TCell this[int x, int y]
        {
            get
            {
                ValidateBounds(x, y);
                return _cells[ToIndex(x, y)];
            }
            set
            {
                ValidateBounds(x, y);
                SetUnchecked(x, y, value, notify: true);
            }
        }

        public TCell this[GridCoord coord]
        {
            get => this[coord.X, coord.Y];
            set => this[coord.X, coord.Y] = value;
        }

        public bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        public bool IsInBounds(GridCoord coord) => IsInBounds(coord.X, coord.Y);

        public bool TryGet(GridCoord coord, out TCell value)
        {
            if (!IsInBounds(coord))
            {
                value = default;
                return false;
            }

            value = _cells[ToIndex(coord.X, coord.Y)];
            return true;
        }

        public bool TrySet(GridCoord coord, TCell value)
        {
            if (!IsInBounds(coord))
                return false;

            SetUnchecked(coord.X, coord.Y, value, notify: true);
            return true;
        }

        public void Fill(TCell value, bool notify = true)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    SetUnchecked(x, y, value, notify);
                }
            }
        }

        public void Fill(Func<GridCoord, TCell> factory, bool notify = true)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    SetUnchecked(x, y, factory(new GridCoord(x, y)), notify);
                }
            }
        }

        public void GetNeighbors(GridCoord coord, List<GridCoord> results, bool includeDiagonals = false)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();

            GridCoord[] directions = includeDiagonals
                ? GridCoord.AllDirections
                : GridCoord.CardinalDirections;

            for (int i = 0; i < directions.Length; i++)
            {
                GridCoord next = coord + directions[i];
                if (IsInBounds(next))
                {
                    results.Add(next);
                }
            }
        }

        public int ToIndex(int x, int y) => y * Width + x;
        public int ToIndex(GridCoord coord) => coord.Y * Width + coord.X;

        public GridCoord FromIndex(int index) => new GridCoord(index % Width, index / Width);

        private void SetUnchecked(int x, int y, TCell value, bool notify)
        {
            int index = ToIndex(x, y);
            TCell oldValue = _cells[index];
            _cells[index] = value;

            if (notify && OnCellChanged != null && !EqualityComparer<TCell>.Default.Equals(oldValue, value))
            {
                OnCellChanged.Invoke(new GridCoord(x, y), oldValue, value);
            }
        }

        private void ValidateBounds(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                throw new ArgumentOutOfRangeException(
                    $"Coordinate ({x}, {y}) is outside grid bounds ({Width}x{Height}).");
            }
        }
    }
}
