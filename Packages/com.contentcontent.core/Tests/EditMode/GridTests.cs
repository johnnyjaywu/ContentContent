using System.Collections.Generic;
using NUnit.Framework;

namespace ContentContent.Core.Grid.Tests
{
    public class GridTests
    {
        [Test]
        public void Grid2D_GetSetAndOnCellChanged()
        {
            var grid = new Grid2D<int>(4, 3);
            GridCoord changedCoord = default;
            int oldVal = -1;
            int newVal = -1;

            grid.OnCellChanged += (coord, oldV, newV) =>
            {
                changedCoord = coord;
                oldVal = oldV;
                newVal = newV;
            };

            grid[2, 1] = 42;

            Assert.AreEqual(42, grid[new GridCoord(2, 1)]);
            Assert.AreEqual(new GridCoord(2, 1), changedCoord);
            Assert.AreEqual(0, oldVal);
            Assert.AreEqual(42, newVal);
            Assert.IsFalse(grid.TryGet(new GridCoord(9, 9), out _));
        }

        [Test]
        public void Grid2D_GetNeighbors_ClampsAtCorners()
        {
            var grid = new Grid2D<int>(5, 5);
            var neighbors = new List<GridCoord>();

            grid.GetNeighbors(new GridCoord(0, 0), neighbors, includeDiagonals: false);
            Assert.AreEqual(2, neighbors.Count);

            grid.GetNeighbors(new GridCoord(0, 0), neighbors, includeDiagonals: true);
            Assert.AreEqual(3, neighbors.Count);

            grid.GetNeighbors(new GridCoord(2, 2), neighbors, includeDiagonals: true);
            Assert.AreEqual(8, neighbors.Count);
        }
    }
}
