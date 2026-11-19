using System.Collections.Generic;
using ContentContent.Core.Grid;
using NUnit.Framework;

namespace ContentContent.Navigation.Tests
{
    public class NavPathfinderTests
    {
        [Test]
        public void PlanarXY_FindsPathAroundWallAndSmooths()
        {
            var grid = new NavGrid(10, 10, 1, NavMode.PlanarXY);
            for (int y = 0; y <= 6; y++)
                grid.SetWalkable(5, y, 0, false);

            var pathfinder = new NavPathfinder();
            var raw = new List<GridCoord3D>();
            var smoothed = new List<GridCoord3D>();

            var start = new GridCoord3D(1, 1, 0);
            var goal = new GridCoord3D(8, 1, 0);

            Assert.IsTrue(pathfinder.TryFindPath(grid, start, goal, raw, allowDiagonals: true, smoothPath: false));
            Assert.IsTrue(pathfinder.TryFindPath(grid, start, goal, smoothed, allowDiagonals: true, smoothPath: true));

            Assert.AreEqual(start, smoothed[0]);
            Assert.AreEqual(goal, smoothed[smoothed.Count - 1]);
            Assert.Less(smoothed.Count, raw.Count, "Smoothed path should collapse collinear/LOS segments.");

            for (int i = 0; i < smoothed.Count - 1; i++)
            {
                Assert.IsTrue(grid.HasLineOfSight(smoothed[i], smoothed[i + 1], preventCornerCutting: true));
            }
        }

        [Test]
        public void PlanarXZ_FindsPathAroundWall()
        {
            var grid = new NavGrid(8, 1, 8, NavMode.PlanarXZ);
            for (int z = 0; z <= 5; z++)
                grid.SetWalkable(4, 0, z, false);

            var pathfinder = new NavPathfinder();
            var path = new List<GridCoord3D>();
            var start = new GridCoord3D(1, 0, 1);
            var goal = new GridCoord3D(6, 0, 1);

            Assert.IsTrue(pathfinder.TryFindPath(grid, start, goal, path, allowDiagonals: true, smoothPath: false));
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);

            for (int i = 0; i < path.Count; i++)
            {
                Assert.AreEqual(0, path[i].Y);
                Assert.IsTrue(grid.IsWalkable(path[i]));
            }
        }

        [Test]
        public void Volumetric3D_NavigatesAroundFloorAndCeilingBarriers()
        {
            // 6x6x6 room with a solid horizontal floor at y = 2 except an opening at (5, 2, 5)
            var grid = new NavGrid(6, 6, 6, NavMode.Volumetric3D);
            for (int z = 0; z < 6; z++)
            {
                for (int x = 0; x < 6; x++)
                {
                    if (x == 5 && z == 5)
                        continue;
                    grid.SetWalkable(x, 2, z, false);
                }
            }

            var pathfinder = new NavPathfinder();
            var path = new List<GridCoord3D>();
            var start = new GridCoord3D(0, 0, 0);
            var goal = new GridCoord3D(0, 5, 0);

            Assert.IsTrue(pathfinder.TryFindPath(grid, start, goal, path, allowDiagonals: true, smoothPath: false));
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);
            Assert.Contains(new GridCoord3D(5, 2, 5), path);

            for (int i = 0; i < path.Count; i++)
                Assert.IsTrue(grid.IsWalkable(path[i]));
        }

        [Test]
        public void Volumetric3D_PreventsSpaceDiagonalCornerCutting()
        {
            // 2x2x2 cube where only (0,0,0) and (1,1,1) are walkable
            var grid = new NavGrid(2, 2, 2, NavMode.Volumetric3D, initialCost: NavGrid.BlockedCost);
            grid.SetWalkable(0, 0, 0, true);
            grid.SetWalkable(1, 1, 1, true);

            var pathfinder = new NavPathfinder();
            var path = new List<GridCoord3D>();

            Assert.IsFalse(pathfinder.TryFindPath(
                grid,
                new GridCoord3D(0, 0, 0),
                new GridCoord3D(1, 1, 1),
                path,
                allowDiagonals: true,
                preventCornerCutting: true));

            // With corner-cutting prevention disabled, diagonal-only connection must succeed
            Assert.IsTrue(pathfinder.TryFindPath(
                grid,
                new GridCoord3D(0, 0, 0),
                new GridCoord3D(1, 1, 1),
                path,
                allowDiagonals: true,
                preventCornerCutting: false));
            Assert.AreEqual(2, path.Count);
        }

        [Test]
        public void StartEqualsGoal_HandlesWalkableAndBlocked()
        {
            var grid = new NavGrid(3, 3, 1, NavMode.PlanarXY);
            var pathfinder = new NavPathfinder();
            var path = new List<GridCoord3D>();

            var cell = new GridCoord3D(1, 1, 0);
            Assert.IsTrue(pathfinder.TryFindPath(grid, cell, cell, path));
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(cell, path[0]);

            grid.SetWalkable(cell, false);
            Assert.IsFalse(pathfinder.TryFindPath(grid, cell, cell, path));
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void ReusablePathfinder_HandlesManyQueriesAndGridResizeCleanly()
        {
            var pathfinder = new NavPathfinder();
            var path = new List<GridCoord3D>();

            var smallGrid = new NavGrid(5, 5, 1, NavMode.PlanarXY);
            var largeGrid = new NavGrid(12, 12, 4, NavMode.Volumetric3D);

            for (int i = 0; i < 25; i++)
            {
                Assert.IsTrue(pathfinder.TryFindPath(
                    smallGrid,
                    new GridCoord3D(0, 0, 0),
                    new GridCoord3D(4, 4, 0),
                    path));
                Assert.AreEqual(new GridCoord3D(4, 4, 0), path[path.Count - 1]);

                Assert.IsTrue(pathfinder.TryFindPath(
                    largeGrid,
                    new GridCoord3D(0, 0, 0),
                    new GridCoord3D(11, 11, 3),
                    path));
                Assert.AreEqual(new GridCoord3D(11, 11, 3), path[path.Count - 1]);
            }
        }
    }
}
