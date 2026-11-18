using System;
using System.Collections.Generic;

namespace ContentContent.Grid
{
    /// <summary>
    /// Generic A* pathfinder operating on any <see cref="IGrid{TCell}"/>.
    /// Supports 4-way and 8-way movement, custom terrain costs, and diagonal corner-cutting prevention.
    /// </summary>
    public static class GridPathfinder
    {
        private const float DiagonalCost = 1.41421356f;

        /// <summary>
        /// Attempts to find a shortest path from <paramref name="start"/> to <paramref name="goal"/>.
        /// Populates <paramref name="outPath"/> from start to goal (inclusive) when successful.
        /// </summary>
        public static bool TryFindPath<TCell>(
            IGrid<TCell> grid,
            GridCoord start,
            GridCoord goal,
            Func<GridCoord, TCell, bool> isWalkable,
            List<GridCoord> outPath,
            Func<GridCoord, TCell, float> getStepCost = null,
            bool allowDiagonals = false,
            bool preventCornerCutting = true)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (isWalkable == null) throw new ArgumentNullException(nameof(isWalkable));
            if (outPath == null) throw new ArgumentNullException(nameof(outPath));

            outPath.Clear();

            if (!grid.IsInBounds(start) || !grid.IsInBounds(goal))
                return false;

            if (!isWalkable(goal, grid[goal]))
                return false;

            if (start == goal)
            {
                outPath.Add(start);
                return true;
            }

            var openHeap = new MinHeap();
            var cameFrom = new Dictionary<GridCoord, GridCoord>();
            var gScore = new Dictionary<GridCoord, float> { [start] = 0f };
            var closed = new HashSet<GridCoord>();
            var directions = allowDiagonals ? GridCoord.AllDirections : GridCoord.CardinalDirections;

            openHeap.Push(start, Heuristic(start, goal, allowDiagonals));

            while (openHeap.Count > 0)
            {
                GridCoord current = openHeap.Pop();
                if (!closed.Add(current))
                    continue;

                if (current == goal)
                {
                    ReconstructPath(cameFrom, start, goal, outPath);
                    return true;
                }

                float currentG = gScore[current];

                for (int i = 0; i < directions.Length; i++)
                {
                    GridCoord dir = directions[i];
                    GridCoord neighbor = current + dir;

                    if (!grid.IsInBounds(neighbor) || closed.Contains(neighbor))
                        continue;

                    TCell neighborCell = grid[neighbor];
                    if (!isWalkable(neighbor, neighborCell))
                        continue;

                    bool isDiagonal = dir.X != 0 && dir.Y != 0;
                    if (isDiagonal && preventCornerCutting)
                    {
                        var adjA = new GridCoord(current.X + dir.X, current.Y);
                        var adjB = new GridCoord(current.X, current.Y + dir.Y);
                        if (!isWalkable(adjA, grid[adjA]) || !isWalkable(adjB, grid[adjB]))
                            continue;
                    }

                    float terrainCost = getStepCost != null ? Math.Max(0.0001f, getStepCost(neighbor, neighborCell)) : 1f;
                    float moveCost = (isDiagonal ? DiagonalCost : 1f) * terrainCost;
                    float tentativeG = currentG + moveCost;

                    if (!gScore.TryGetValue(neighbor, out float existingG) || tentativeG < existingG)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        float fScore = tentativeG + Heuristic(neighbor, goal, allowDiagonals);
                        openHeap.Push(neighbor, fScore);
                    }
                }
            }

            return false;
        }

        private static float Heuristic(GridCoord a, GridCoord b, bool allowDiagonals)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            if (!allowDiagonals)
                return dx + dy;

            int min = Math.Min(dx, dy);
            int max = Math.Max(dx, dy);
            return min * DiagonalCost + (max - min);
        }

        private static void ReconstructPath(
            Dictionary<GridCoord, GridCoord> cameFrom,
            GridCoord start,
            GridCoord goal,
            List<GridCoord> outPath)
        {
            GridCoord cursor = goal;
            outPath.Add(cursor);

            while (cursor != start)
            {
                cursor = cameFrom[cursor];
                outPath.Add(cursor);
            }

            outPath.Reverse();
        }

        private sealed class MinHeap
        {
            private readonly List<(GridCoord Coord, float Priority)> _items = new List<(GridCoord, float)>();

            public int Count => _items.Count;

            public void Push(GridCoord coord, float priority)
            {
                _items.Add((coord, priority));
                int child = _items.Count - 1;
                while (child > 0)
                {
                    int parent = (child - 1) >> 1;
                    if (_items[child].Priority >= _items[parent].Priority)
                        break;

                    (_items[child], _items[parent]) = (_items[parent], _items[child]);
                    child = parent;
                }
            }

            public GridCoord Pop()
            {
                GridCoord top = _items[0].Coord;
                int last = _items.Count - 1;
                _items[0] = _items[last];
                _items.RemoveAt(last);

                int parent = 0;
                int count = _items.Count;
                while (true)
                {
                    int left = (parent << 1) + 1;
                    if (left >= count)
                        break;

                    int right = left + 1;
                    int smallest = (right < count && _items[right].Priority < _items[left].Priority) ? right : left;

                    if (_items[parent].Priority <= _items[smallest].Priority)
                        break;

                    (_items[parent], _items[smallest]) = (_items[smallest], _items[parent]);
                    parent = smallest;
                }

                return top;
            }
        }
    }
}
