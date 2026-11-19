using ContentContent.Core.Grid;
using NUnit.Framework;

namespace ContentContent.Navigation.Tests
{
    public class NavGridTests
    {
        [Test]
        public void NavGrid_PlanarModesClampUnusedAxisToOne()
        {
            var xy = new NavGrid(8, 6, 10, NavMode.PlanarXY);
            Assert.AreEqual(8, xy.Width);
            Assert.AreEqual(6, xy.Height);
            Assert.AreEqual(1, xy.Depth);

            var xz = new NavGrid(8, 10, 6, NavMode.PlanarXZ);
            Assert.AreEqual(8, xz.Width);
            Assert.AreEqual(1, xz.Height);
            Assert.AreEqual(6, xz.Depth);

            var vol = new NavGrid(4, 5, 6, NavMode.Volumetric3D);
            Assert.AreEqual(4, vol.Width);
            Assert.AreEqual(5, vol.Height);
            Assert.AreEqual(6, vol.Depth);
        }

        [Test]
        public void NavGrid_AreConnected_UpdatesAfterObstacleAddedAndRemoved()
        {
            var grid = new NavGrid(5, 1, 5, NavMode.PlanarXZ);
            var a = new GridCoord3D(0, 0, 0);
            var b = new GridCoord3D(4, 0, 4);

            Assert.IsTrue(grid.AreConnected(a, b));

            // Seal the grid with a wall at x = 2
            for (int z = 0; z < 5; z++)
                grid.SetWalkable(2, 0, z, false);

            Assert.IsFalse(grid.AreConnected(a, b));

            // Open a doorway
            grid.SetWalkable(2, 0, 2, true);
            Assert.IsTrue(grid.AreConnected(a, b));
        }

        [Test]
        public void NavGrid_HasLineOfSight_RespectsCornersAndObstacles()
        {
            var grid = new NavGrid(4, 4, 1, NavMode.PlanarXY);
            Assert.IsTrue(grid.HasLineOfSight(new GridCoord3D(0, 0, 0), new GridCoord3D(3, 3, 0)));

            // Block two adjacent cells forming a diagonal pinch at (1,0) and (0,1)
            grid.SetWalkable(1, 0, 0, false);
            grid.SetWalkable(0, 1, 0, false);

            Assert.IsFalse(grid.HasLineOfSight(new GridCoord3D(0, 0, 0), new GridCoord3D(1, 1, 0), preventCornerCutting: true));
            Assert.IsTrue(grid.HasLineOfSight(new GridCoord3D(0, 0, 0), new GridCoord3D(1, 1, 0), preventCornerCutting: false));
        }

        [Test]
        public void NavGrid_TryFindNearestWalkable_SnapsFromBlockedOrOutOfBounds()
        {
            var grid = new NavGrid(5, 5, 5, NavMode.Volumetric3D);
            grid.SetWalkable(2, 2, 2, false);

            Assert.IsTrue(grid.TryFindNearestWalkable(new GridCoord3D(2, 2, 2), maxRadius: 3, out var nearest));
            Assert.IsTrue(grid.IsWalkable(nearest));
            Assert.AreEqual(1, GridCoord3D.ManhattanDistance(new GridCoord3D(2, 2, 2), nearest));

            Assert.IsTrue(grid.TryFindNearestWalkable(new GridCoord3D(-5, 2, 2), maxRadius: 2, out var clamped));
            Assert.AreEqual(new GridCoord3D(0, 2, 2), clamped);
        }
    }
}
