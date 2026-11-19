using System.Collections.Generic;
using ContentContent.Core.Grid;
using NUnit.Framework;

namespace ContentContent.Navigation.Tests
{
    public class NavPathQueueTests
    {
        [Test]
        public void ProcessBudget_LimitsRequestsPerStepAndDeduplicatesPerOwner()
        {
            var grid = new NavGrid(8, 1, 8, NavMode.PlanarXZ);
            var pathfinder = new NavPathfinder();
            var queue = new NavPathQueue();

            var ownerA = new object();
            var ownerB = new object();
            var ownerC = new object();

            GridCoord3D ownerALastGoal = default;
            int callbacks = 0;

            queue.Enqueue(ownerA, new GridCoord3D(0, 0, 0), new GridCoord3D(2, 0, 2), (ok, p) =>
            {
                callbacks++;
                if (ok) ownerALastGoal = p[p.Count - 1];
            });

            // Overwrite ownerA's pending request before processing
            queue.Enqueue(ownerA, new GridCoord3D(0, 0, 0), new GridCoord3D(7, 0, 7), (ok, p) =>
            {
                callbacks++;
                if (ok) ownerALastGoal = p[p.Count - 1];
            });

            queue.Enqueue(ownerB, new GridCoord3D(0, 0, 0), new GridCoord3D(4, 0, 4), (_, __) => callbacks++);
            queue.Enqueue(ownerC, new GridCoord3D(0, 0, 0), new GridCoord3D(5, 0, 5), (_, __) => callbacks++);

            Assert.AreEqual(3, queue.PendingCount);

            int step1 = queue.ProcessBudget(grid, pathfinder, maxRequests: 2);
            Assert.AreEqual(2, step1);
            Assert.AreEqual(2, callbacks);
            Assert.AreEqual(new GridCoord3D(7, 0, 7), ownerALastGoal);
            Assert.AreEqual(1, queue.PendingCount);

            int step2 = queue.ProcessBudget(grid, pathfinder, maxRequests: 2);
            Assert.AreEqual(1, step2);
            Assert.AreEqual(3, callbacks);
            Assert.AreEqual(0, queue.PendingCount);
        }
    }
}
