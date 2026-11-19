using System.Collections.Generic;
using NUnit.Framework;

namespace ContentContent.Core.Grid.Tests
{
    public class Grid3DTests
    {
        [Test]
        public void Grid3D_GetSetAndOnCellChanged()
        {
            var grid = new Grid3D<int>(4, 3, 2);
            GridCoord3D changedCoord = default;
            int oldVal = -1;
            int newVal = -1;

            grid.OnCellChanged += (coord, oldV, newV) =>
            {
                changedCoord = coord;
                oldVal = oldV;
                newVal = newV;
            };

            grid[2, 1, 1] = 99;

            Assert.AreEqual(24, grid.Count);
            Assert.AreEqual(99, grid[new GridCoord3D(2, 1, 1)]);
            Assert.AreEqual(new GridCoord3D(2, 1, 1), changedCoord);
            Assert.AreEqual(0, oldVal);
            Assert.AreEqual(99, newVal);
            Assert.IsFalse(grid.TryGet(new GridCoord3D(9, 9, 9), out _));
        }

        [Test]
        public void Grid3D_ToIndexAndFromIndex_RoundTrip()
        {
            var grid = new Grid3D<byte>(5, 4, 3);
            for (int z = 0; z < grid.Depth; z++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    for (int x = 0; x < grid.Width; x++)
                    {
                        var coord = new GridCoord3D(x, y, z);
                        int index = grid.ToIndex(coord);
                        Assert.AreEqual(coord, grid.FromIndex(index));
                    }
                }
            }
        }

        [Test]
        public void Grid3D_GetNeighbors_ClampsAtCornersAndHandlesCenter()
        {
            var grid = new Grid3D<int>(3, 3, 3);
            var neighbors = new List<GridCoord3D>();

            grid.GetNeighbors(new GridCoord3D(0, 0, 0), neighbors, includeDiagonals: false);
            Assert.AreEqual(3, neighbors.Count);

            grid.GetNeighbors(new GridCoord3D(0, 0, 0), neighbors, includeDiagonals: true);
            Assert.AreEqual(7, neighbors.Count);

            grid.GetNeighbors(new GridCoord3D(1, 1, 1), neighbors, includeDiagonals: false);
            Assert.AreEqual(6, neighbors.Count);

            grid.GetNeighbors(new GridCoord3D(1, 1, 1), neighbors, includeDiagonals: true);
            Assert.AreEqual(26, neighbors.Count);
        }
    }
}
