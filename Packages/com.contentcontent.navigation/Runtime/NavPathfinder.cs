using System;
using System.Collections.Generic;
using ContentContent.Core.Grid;

namespace ContentContent.Navigation
{
    /// <summary>
    /// Reusable, zero-allocation A* pathfinder operating on a <see cref="NavGrid"/> in
    /// 2D (<see cref="NavMode.PlanarXY"/>, <see cref="NavMode.PlanarXZ"/>) or
    /// volumetric 3D (<see cref="NavMode.Volumetric3D"/>).
    /// Supports generation-stamped scratch arrays, diagonal corner-cutting prevention,
    /// connected-region fast rejection, and line-of-sight string-pulling path smoothing.
    /// </summary>
    public sealed class NavPathfinder
    {
        private const float Sqrt2 = 1.41421356f;
        private const float Sqrt3 = 1.73205081f;

        private static readonly GridCoord3D[] DirectionsXY4 =
        {
            new GridCoord3D(1, 0, 0), new GridCoord3D(-1, 0, 0),
            new GridCoord3D(0, 1, 0), new GridCoord3D(0, -1, 0)
        };

        private static readonly GridCoord3D[] DirectionsXY8 =
        {
            new GridCoord3D(1, 0, 0), new GridCoord3D(-1, 0, 0),
            new GridCoord3D(0, 1, 0), new GridCoord3D(0, -1, 0),
            new GridCoord3D(1, 1, 0), new GridCoord3D(1, -1, 0),
            new GridCoord3D(-1, 1, 0), new GridCoord3D(-1, -1, 0)
        };

        private static readonly GridCoord3D[] DirectionsXZ4 =
        {
            new GridCoord3D(1, 0, 0), new GridCoord3D(-1, 0, 0),
            new GridCoord3D(0, 0, 1), new GridCoord3D(0, 0, -1)
        };

        private static readonly GridCoord3D[] DirectionsXZ8 =
        {
            new GridCoord3D(1, 0, 0), new GridCoord3D(-1, 0, 0),
            new GridCoord3D(0, 0, 1), new GridCoord3D(0, 0, -1),
            new GridCoord3D(1, 0, 1), new GridCoord3D(1, 0, -1),
            new GridCoord3D(-1, 0, 1), new GridCoord3D(-1, 0, -1)
        };

        private float[] _gScore = Array.Empty<float>();
        private int[] _cameFrom = Array.Empty<int>();
        private int[] _closedStamp = Array.Empty<int>();
        private int[] _seenStamp = Array.Empty<int>();
        private int _searchId;

        private readonly IndexMinHeap _openHeap = new IndexMinHeap();
        private readonly List<GridCoord3D> _rawPathBuffer = new List<GridCoord3D>(64);

        /// <summary>
        /// Attempts to find a shortest path from <paramref name="start"/> to <paramref name="goal"/>.
        /// Populates <paramref name="outPath"/> from start to goal (inclusive) on success.
        /// </summary>
        public bool TryFindPath(
            NavGrid grid,
            GridCoord3D start,
            GridCoord3D goal,
            List<GridCoord3D> outPath,
            bool allowDiagonals = true,
            bool preventCornerCutting = true,
            bool smoothPath = true,
            int maxExpansions = 0)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (outPath == null) throw new ArgumentNullException(nameof(outPath));

            outPath.Clear();

            if (!grid.IsInBounds(start) || !grid.IsInBounds(goal))
                return false;

            if (!grid.IsWalkable(start) || !grid.IsWalkable(goal))
                return false;

            if (start == goal)
            {
                outPath.Add(start);
                return true;
            }

            // Corner-safe paths are always cardinally connected, so we can reject unreachable goals in O(1).
            if ((!allowDiagonals || preventCornerCutting) && !grid.AreConnected(start, goal))
                return false;

            EnsureBuffers(grid.Count);
            int searchId = NextSearchId();

            int startIdx = grid.ToIndex(start);
            int goalIdx = grid.ToIndex(goal);

            _openHeap.Clear();
            _seenStamp[startIdx] = searchId;
            _gScore[startIdx] = 0f;
            _cameFrom[startIdx] = startIdx;
            _openHeap.Push(startIdx, Heuristic(start, goal, grid.Mode, allowDiagonals));

            GridCoord3D[] directions = GetDirections(grid.Mode, allowDiagonals);
            int expansions = 0;

            while (_openHeap.Count > 0)
            {
                int currentIdx = _openHeap.Pop();
                if (_closedStamp[currentIdx] == searchId)
                    continue;

                _closedStamp[currentIdx] = searchId;

                if (currentIdx == goalIdx)
                {
                    if (smoothPath)
                    {
                        ReconstructPath(grid, startIdx, goalIdx, _rawPathBuffer);
                        SmoothPath(grid, _rawPathBuffer, outPath, preventCornerCutting);
                    }
                    else
                    {
                        ReconstructPath(grid, startIdx, goalIdx, outPath);
                    }
                    return true;
                }

                if (maxExpansions > 0 && ++expansions >= maxExpansions)
                    return false;

                GridCoord3D current = grid.FromIndex(currentIdx);
                float currentG = _gScore[currentIdx];

                for (int i = 0; i < directions.Length; i++)
                {
                    GridCoord3D dir = directions[i];
                    int nx = current.X + dir.X;
                    int ny = current.Y + dir.Y;
                    int nz = current.Z + dir.Z;

                    if (!grid.IsInBounds(nx, ny, nz))
                        continue;

                    int neighborIdx = grid.ToIndex(nx, ny, nz);
                    if (_closedStamp[neighborIdx] == searchId)
                        continue;

                    byte cellCost = grid.GetCostUnchecked(neighborIdx);
                    if (cellCost == NavGrid.BlockedCost)
                        continue;

                    int axes = (dir.X != 0 ? 1 : 0) + (dir.Y != 0 ? 1 : 0) + (dir.Z != 0 ? 1 : 0);
                    if (axes > 1 && preventCornerCutting && !IsDiagonalClear(grid, current, dir, axes))
                        continue;

                    float stepLength = axes == 1 ? 1f : (axes == 2 ? Sqrt2 : Sqrt3);
                    float tentativeG = currentG + stepLength * cellCost;

                    if (_seenStamp[neighborIdx] != searchId || tentativeG < _gScore[neighborIdx])
                    {
                        _seenStamp[neighborIdx] = searchId;
                        _gScore[neighborIdx] = tentativeG;
                        _cameFrom[neighborIdx] = currentIdx;

                        float f = tentativeG + Heuristic(new GridCoord3D(nx, ny, nz), goal, grid.Mode, allowDiagonals);
                        _openHeap.Push(neighborIdx, f);
                    }
                }
            }

            return false;
        }

        private static bool IsDiagonalClear(NavGrid grid, GridCoord3D c, GridCoord3D dir, int axes)
        {
            if (dir.X != 0 && !grid.IsWalkable(c.X + dir.X, c.Y, c.Z)) return false;
            if (dir.Y != 0 && !grid.IsWalkable(c.X, c.Y + dir.Y, c.Z)) return false;
            if (dir.Z != 0 && !grid.IsWalkable(c.X, c.Y, c.Z + dir.Z)) return false;

            if (axes == 3)
            {
                if (!grid.IsWalkable(c.X + dir.X, c.Y + dir.Y, c.Z)) return false;
                if (!grid.IsWalkable(c.X + dir.X, c.Y, c.Z + dir.Z)) return false;
                if (!grid.IsWalkable(c.X, c.Y + dir.Y, c.Z + dir.Z)) return false;
            }

            return true;
        }

        private static GridCoord3D[] GetDirections(NavMode mode, bool allowDiagonals)
        {
            switch (mode)
            {
                case NavMode.PlanarXY:
                    return allowDiagonals ? DirectionsXY8 : DirectionsXY4;
                case NavMode.PlanarXZ:
                    return allowDiagonals ? DirectionsXZ8 : DirectionsXZ4;
                default:
                    return allowDiagonals ? GridCoord3D.AllDirections : GridCoord3D.CardinalDirections;
            }
        }

        private static float Heuristic(GridCoord3D a, GridCoord3D b, NavMode mode, bool allowDiagonals)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            int dz = Math.Abs(a.Z - b.Z);

            if (!allowDiagonals)
                return dx + dy + dz;

            if (mode == NavMode.PlanarXY)
            {
                int min = Math.Min(dx, dy);
                int max = Math.Max(dx, dy);
                return min * Sqrt2 + (max - min);
            }

            if (mode == NavMode.PlanarXZ)
            {
                int min = Math.Min(dx, dz);
                int max = Math.Max(dx, dz);
                return min * Sqrt2 + (max - min);
            }

            // 3D Octile distance: sort dx, dy, dz ascending (d0 <= d1 <= d2).
            int d0 = dx, d1 = dy, d2 = dz;
            if (d0 > d1) (d0, d1) = (d1, d0);
            if (d1 > d2) (d1, d2) = (d2, d1);
            if (d0 > d1) (d0, d1) = (d1, d0);

            return d0 * Sqrt3 + (d1 - d0) * Sqrt2 + (d2 - d1);
        }

        private void ReconstructPath(NavGrid grid, int startIdx, int goalIdx, List<GridCoord3D> target)
        {
            target.Clear();
            int cursor = goalIdx;
            target.Add(grid.FromIndex(cursor));

            while (cursor != startIdx)
            {
                cursor = _cameFrom[cursor];
                target.Add(grid.FromIndex(cursor));
            }

            target.Reverse();
        }

        private static void SmoothPath(NavGrid grid, List<GridCoord3D> raw, List<GridCoord3D> smoothed, bool preventCornerCutting)
        {
            smoothed.Clear();
            int count = raw.Count;
            if (count <= 2)
            {
                for (int i = 0; i < count; i++)
                    smoothed.Add(raw[i]);
                return;
            }

            int anchor = 0;
            smoothed.Add(raw[0]);

            while (anchor < count - 1)
            {
                int furthest = anchor + 1;
                byte maxCostAlongSegment = Math.Max(grid.GetCost(raw[anchor]), grid.GetCost(raw[furthest]));

                for (int probe = anchor + 2; probe < count; probe++)
                {
                    byte probeCost = grid.GetCost(raw[probe]);
                    byte candidateMax = Math.Max(maxCostAlongSegment, probeCost);

                    if (grid.HasLineOfSight(raw[anchor], raw[probe], preventCornerCutting, candidateMax))
                    {
                        furthest = probe;
                        maxCostAlongSegment = candidateMax;
                    }
                }

                smoothed.Add(raw[furthest]);
                anchor = furthest;
            }
        }

        private void EnsureBuffers(int requiredSize)
        {
            if (_gScore.Length >= requiredSize)
                return;

            _gScore = new float[requiredSize];
            _cameFrom = new int[requiredSize];
            _closedStamp = new int[requiredSize];
            _seenStamp = new int[requiredSize];
            _searchId = 0;
        }

        private int NextSearchId()
        {
            if (_searchId == int.MaxValue)
            {
                Array.Clear(_closedStamp, 0, _closedStamp.Length);
                Array.Clear(_seenStamp, 0, _seenStamp.Length);
                _searchId = 0;
            }

            return ++_searchId;
        }

        private sealed class IndexMinHeap
        {
            private int[] _indices = new int[64];
            private float[] _priorities = new float[64];
            private int _count;

            public int Count => _count;

            public void Clear() => _count = 0;

            public void Push(int cellIndex, float priority)
            {
                if (_count == _indices.Length)
                {
                    int newCapacity = _indices.Length * 2;
                    Array.Resize(ref _indices, newCapacity);
                    Array.Resize(ref _priorities, newCapacity);
                }

                int child = _count++;
                _indices[child] = cellIndex;
                _priorities[child] = priority;

                while (child > 0)
                {
                    int parent = (child - 1) >> 1;
                    if (_priorities[child] >= _priorities[parent])
                        break;

                    (_indices[child], _indices[parent]) = (_indices[parent], _indices[child]);
                    (_priorities[child], _priorities[parent]) = (_priorities[parent], _priorities[child]);
                    child = parent;
                }
            }

            public int Pop()
            {
                int top = _indices[0];
                _count--;

                if (_count > 0)
                {
                    _indices[0] = _indices[_count];
                    _priorities[0] = _priorities[_count];

                    int parent = 0;
                    while (true)
                    {
                        int left = (parent << 1) + 1;
                        if (left >= _count)
                            break;

                        int right = left + 1;
                        int smallest = (right < _count && _priorities[right] < _priorities[left]) ? right : left;

                        if (_priorities[parent] <= _priorities[smallest])
                            break;

                        (_indices[parent], _indices[smallest]) = (_indices[smallest], _indices[parent]);
                        (_priorities[parent], _priorities[smallest]) = (_priorities[smallest], _priorities[parent]);
                        parent = smallest;
                    }
                }

                return top;
            }
        }
    }
}
