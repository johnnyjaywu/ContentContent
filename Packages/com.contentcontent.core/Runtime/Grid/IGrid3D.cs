using System;
using System.Collections.Generic;

namespace ContentContent.Core.Grid
{
    /// <summary>
    /// Generic contract for a bounded 3D grid of <typeparamref name="TCell"/> values.
    /// </summary>
    public interface IGrid3D<TCell>
    {
        int Width { get; }
        int Height { get; }
        int Depth { get; }
        int Count { get; }

        TCell this[int x, int y, int z] { get; set; }
        TCell this[GridCoord3D coord] { get; set; }

        /// <summary>Invoked when a cell value is updated: (coord, oldValue, newValue).</summary>
        event Action<GridCoord3D, TCell, TCell> OnCellChanged;

        bool IsInBounds(int x, int y, int z);
        bool IsInBounds(GridCoord3D coord);

        bool TryGet(GridCoord3D coord, out TCell value);
        bool TrySet(GridCoord3D coord, TCell value);

        void GetNeighbors(GridCoord3D coord, List<GridCoord3D> results, bool includeDiagonals = false);
    }
}
