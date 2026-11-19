using System;
using System.Collections.Generic;
using ContentContent.Core.Grid;

namespace ContentContent.Navigation
{
    /// <summary>
    /// Pure-C# per-frame budgeted path request queue. Deduplicates pending requests per owner
    /// so many agents repathing simultaneously share a smooth per-frame CPU budget.
    /// </summary>
    public sealed class NavPathQueue
    {
        private struct Request
        {
            public object Owner;
            public GridCoord3D Start;
            public GridCoord3D Goal;
            public bool AllowDiagonals;
            public bool PreventCornerCutting;
            public bool SmoothPath;
            public Action<bool, List<GridCoord3D>> Callback;
        }

        private readonly List<Request> _pending = new List<Request>(16);
        private readonly List<GridCoord3D> _resultBuffer = new List<GridCoord3D>(64);

        public int PendingCount => _pending.Count;

        public void Enqueue(
            object owner,
            GridCoord3D start,
            GridCoord3D goal,
            Action<bool, List<GridCoord3D>> callback,
            bool allowDiagonals = true,
            bool preventCornerCutting = true,
            bool smoothPath = true)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var req = new Request
            {
                Owner = owner,
                Start = start,
                Goal = goal,
                AllowDiagonals = allowDiagonals,
                PreventCornerCutting = preventCornerCutting,
                SmoothPath = smoothPath,
                Callback = callback
            };

            for (int i = 0; i < _pending.Count; i++)
            {
                if (ReferenceEquals(_pending[i].Owner, owner))
                {
                    _pending[i] = req;
                    return;
                }
            }

            _pending.Add(req);
        }

        public void Cancel(object owner)
        {
            if (owner == null)
                return;

            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_pending[i].Owner, owner))
                {
                    _pending.RemoveAt(i);
                }
            }
        }

        public void Clear() => _pending.Clear();

        /// <summary>
        /// Processes up to <paramref name="maxRequests"/> queued path queries.
        /// Returns the number of requests processed.
        /// </summary>
        public int ProcessBudget(NavGrid grid, NavPathfinder pathfinder, int maxRequests, int maxExpansionsPerPath = 0)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (pathfinder == null) throw new ArgumentNullException(nameof(pathfinder));

            if (maxRequests <= 0 || _pending.Count == 0)
                return 0;

            int processed = 0;
            while (processed < maxRequests && _pending.Count > 0)
            {
                Request req = _pending[0];
                _pending.RemoveAt(0);

                bool found = pathfinder.TryFindPath(
                    grid,
                    req.Start,
                    req.Goal,
                    _resultBuffer,
                    req.AllowDiagonals,
                    req.PreventCornerCutting,
                    req.SmoothPath,
                    maxExpansionsPerPath);

                req.Callback(found, _resultBuffer);
                processed++;
            }

            return processed;
        }
    }
}
