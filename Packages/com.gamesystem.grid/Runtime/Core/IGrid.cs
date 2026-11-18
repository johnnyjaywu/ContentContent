using System;
using System.Collections.Generic;

namespace GameSystem.Grid
{
    /// <summary>
    /// Generic contract for a bounded 2D grid of <typeparamref name="TCell"/> values.
    /// </summary>
    public interface IGrid<TCell>
    {
        int Width { get; }
        int Height { get; }
        int Count { get; }

        TCell this[int x, int y] { get; set; }
        TCell this[GridCoord coord] { get; set; }

        /// <summary>Invoked when a cell value is updated: (coord, oldValue, newValue).</summary>
        event Action<GridCoord, TCell, TCell> OnCellChanged;

        bool IsInBounds(int x, int y);
        bool IsInBounds(GridCoord coord);

        bool TryGet(GridCoord coord, out TCell value);
        bool TrySet(GridCoord coord, TCell value);

        void GetNeighbors(GridCoord coord, List<GridCoord> results, bool includeDiagonals = false);
    }
}
