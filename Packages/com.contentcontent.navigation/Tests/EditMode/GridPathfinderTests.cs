using System.Collections.Generic;
using ContentContent.Core.Grid;
using NUnit.Framework;

namespace ContentContent.Navigation.Tests
{
    public class GridPathfinderTests
    {
        [Test]
        public void Pathfinder_AvoidsObstaclesAndFindsShortestPath()
        {
            // 5x5 grid with a vertical wall at x = 2, y = 0..3 (gap at y = 4)
            var grid = new Grid2D<bool>(5, 5, _ => true);
            for (int y = 0; y <= 3; y++)
            {
                grid[2, y] = false;
            }

            var path = new List<GridCoord>();
            bool found = GridPathfinder.TryFindPath(
                grid,
                start: new GridCoord(0, 0),
                goal: new GridCoord(4, 0),
                isWalkable: (_, walkable) => walkable,
                outPath: path,
                allowDiagonals: false);

            Assert.IsTrue(found);
            Assert.AreEqual(new GridCoord(0, 0), path[0]);
            Assert.AreEqual(new GridCoord(4, 0), path[path.Count - 1]);
            Assert.Contains(new GridCoord(2, 4), path);

            for (int i = 0; i < path.Count; i++)
            {
                Assert.IsTrue(grid[path[i]], $"Path stepped on blocked cell {path[i]}");
            }
        }

        [Test]
        public void Pathfinder_ReturnsFalseWhenGoalUnreachable()
        {
            var grid = new Grid2D<bool>(3, 3, _ => true);
            grid[1, 0] = false;
            grid[1, 1] = false;
            grid[1, 2] = false;

            var path = new List<GridCoord>();
            bool found = GridPathfinder.TryFindPath(
                grid,
                start: new GridCoord(0, 0),
                goal: new GridCoord(2, 2),
                isWalkable: (_, walkable) => walkable,
                outPath: path);

            Assert.IsFalse(found);
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void Pathfinder_PreventsDiagonalCornerCutting()
        {
            // 2x2 grid:
            // (0,1)=blocked, (1,1)=open
            // (0,0)=open,    (1,0)=blocked
            var grid = new Grid2D<bool>(2, 2, _ => true);
            grid[0, 1] = false;
            grid[1, 0] = false;

            var path = new List<GridCoord>();
            bool blockedWithPrevention = GridPathfinder.TryFindPath(
                grid,
                start: new GridCoord(0, 0),
                goal: new GridCoord(1, 1),
                isWalkable: (_, walkable) => walkable,
                outPath: path,
                allowDiagonals: true,
                preventCornerCutting: true);

            Assert.IsFalse(blockedWithPrevention);

            bool allowedWithoutPrevention = GridPathfinder.TryFindPath(
                grid,
                start: new GridCoord(0, 0),
                goal: new GridCoord(1, 1),
                isWalkable: (_, walkable) => walkable,
                outPath: path,
                allowDiagonals: true,
                preventCornerCutting: false);

            Assert.IsTrue(allowedWithoutPrevention);
            Assert.AreEqual(2, path.Count);
        }
    }
}
