using System;
using ContentContent.Core.Grid;

namespace ContentContent.Navigation
{
    /// <summary>
    /// Compact 1-byte-per-cell navigation grid supporting <see cref="NavMode.PlanarXY"/>,
    /// <see cref="NavMode.PlanarXZ"/>, and <see cref="NavMode.Volumetric3D"/>.
    /// Cell value 0 means blocked; 1..255 means walkable with that traversal cost multiplier (1 = normal).
    /// </summary>
    public sealed class NavGrid
    {
        public const byte BlockedCost = 0;
        public const byte DefaultWalkableCost = 1;

        private readonly byte[] _cells;
        private int[] _regionIds;
        private int[] _bfsQueue;
        private int _regionsVersion = -1;

        public NavMode Mode { get; }
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public int Count => _cells.Length;

        /// <summary>
        /// Monotonically incremented whenever any cell walkability or cost changes.
        /// </summary>
        public int Version { get; private set; }

        public NavGrid(int width, int height, int depth, NavMode mode = NavMode.Volumetric3D, byte initialCost = DefaultWalkableCost)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");
            if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be greater than 0.");

            Mode = mode;
            Width = width;
            Height = mode == NavMode.PlanarXZ ? 1 : height;
            Depth = mode == NavMode.PlanarXY ? 1 : depth;

            _cells = new byte[Width * Height * Depth];
            if (initialCost != 0)
            {
                Array.Fill(_cells, initialCost);
            }

            Version = 1;
        }

        public bool IsInBounds(int x, int y, int z)
            => x >= 0 && x < Width && y >= 0 && y < Height && z >= 0 && z < Depth;

        public bool IsInBounds(GridCoord3D coord) => IsInBounds(coord.X, coord.Y, coord.Z);

        public int ToIndex(int x, int y, int z) => x + Width * (y + Height * z);
        public int ToIndex(GridCoord3D coord) => ToIndex(coord.X, coord.Y, coord.Z);

        public GridCoord3D FromIndex(int index)
        {
            int x = index % Width;
            int rem = index / Width;
            int y = rem % Height;
            int z = rem / Height;
            return new GridCoord3D(x, y, z);
        }

        public byte GetCost(int x, int y, int z)
        {
            if (!IsInBounds(x, y, z))
                return BlockedCost;
            return _cells[ToIndex(x, y, z)];
        }

        public byte GetCost(GridCoord3D coord) => GetCost(coord.X, coord.Y, coord.Z);

        internal byte GetCostUnchecked(int index) => _cells[index];

        public bool IsWalkable(int x, int y, int z) => GetCost(x, y, z) != BlockedCost;
        public bool IsWalkable(GridCoord3D coord) => GetCost(coord.X, coord.Y, coord.Z) != BlockedCost;

        public void SetWalkable(int x, int y, int z, bool walkable)
            => SetCost(x, y, z, walkable ? DefaultWalkableCost : BlockedCost);

        public void SetWalkable(GridCoord3D coord, bool walkable)
            => SetCost(coord.X, coord.Y, coord.Z, walkable ? DefaultWalkableCost : BlockedCost);

        public void SetCost(int x, int y, int z, byte cost)
        {
            if (!IsInBounds(x, y, z))
                return;

            int index = ToIndex(x, y, z);
            if (_cells[index] == cost)
                return;

            _cells[index] = cost;
            Version++;
        }

        public void SetCost(GridCoord3D coord, byte cost) => SetCost(coord.X, coord.Y, coord.Z, cost);

        public void Fill(byte cost)
        {
            Array.Fill(_cells, cost);
            Version++;
        }

        /// <summary>
        /// Returns true if <paramref name="a"/> and <paramref name="b"/> belong to the same
        /// cardinally connected walkable region. Rebuilds region IDs lazily when <see cref="Version"/> changes.
        /// </summary>
        public bool AreConnected(GridCoord3D a, GridCoord3D b)
        {
            if (!IsWalkable(a) || !IsWalkable(b))
                return false;

            if (a == b)
                return true;

            EnsureRegionsUpToDate();
            int regA = _regionIds[ToIndex(a)];
            return regA != 0 && regA == _regionIds[ToIndex(b)];
        }

        /// <summary>
        /// Finds the closest walkable cell to <paramref name="origin"/> within <paramref name="maxRadius"/>.
        /// </summary>
        public bool TryFindNearestWalkable(GridCoord3D origin, int maxRadius, out GridCoord3D result)
        {
            int cx = Math.Max(0, Math.Min(Width - 1, origin.X));
            int cy = Math.Max(0, Math.Min(Height - 1, origin.Y));
            int cz = Math.Max(0, Math.Min(Depth - 1, origin.Z));
            var clamped = new GridCoord3D(cx, cy, cz);

            if (IsWalkable(clamped))
            {
                result = clamped;
                return true;
            }

            bool useY = Height > 1;
            bool useZ = Depth > 1;
            int bestDistSq = int.MaxValue;
            GridCoord3D best = default;
            bool found = false;

            for (int r = 1; r <= maxRadius; r++)
            {
                int minX = Math.Max(0, cx - r);
                int maxX = Math.Min(Width - 1, cx + r);
                int minY = useY ? Math.Max(0, cy - r) : cy;
                int maxY = useY ? Math.Min(Height - 1, cy + r) : cy;
                int minZ = useZ ? Math.Max(0, cz - r) : cz;
                int maxZ = useZ ? Math.Min(Depth - 1, cz + r) : cz;

                for (int z = minZ; z <= maxZ; z++)
                {
                    int dz = z - cz;
                    int absDz = Math.Abs(dz);
                    for (int y = minY; y <= maxY; y++)
                    {
                        int dy = y - cy;
                        int absDy = Math.Abs(dy);
                        for (int x = minX; x <= maxX; x++)
                        {
                            int dx = x - cx;
                            int absDx = Math.Abs(dx);

                            // Only inspect the outer shell at Chebyshev radius r.
                            if (absDx != r && (!useY || absDy != r) && (!useZ || absDz != r))
                                continue;

                            if (!IsWalkable(x, y, z))
                                continue;

                            int distSq = dx * dx + dy * dy + dz * dz;
                            if (distSq < bestDistSq)
                            {
                                bestDistSq = distSq;
                                best = new GridCoord3D(x, y, z);
                                found = true;
                            }
                        }
                    }
                }

                if (found)
                {
                    result = best;
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Checks unobstructed straight-line visibility between <paramref name="a"/> and <paramref name="b"/>
        /// using 3D Amanatides-Woo DDA voxel traversal. When <paramref name="maxAllowedCost"/> is specified,
        /// cells with cost greater than <paramref name="maxAllowedCost"/> also break line of sight.
        /// </summary>
        public bool HasLineOfSight(GridCoord3D a, GridCoord3D b, bool preventCornerCutting = true, byte maxAllowedCost = byte.MaxValue)
        {
            if (!IsInBounds(a) || !IsInBounds(b))
                return false;

            byte startCost = GetCost(a);
            byte endCost = GetCost(b);
            if (startCost == BlockedCost || endCost == BlockedCost)
                return false;

            if (a == b)
                return startCost <= maxAllowedCost;

            int x = a.X, y = a.Y, z = a.Z;
            int dx = b.X - a.X;
            int dy = b.Y - a.Y;
            int dz = b.Z - a.Z;

            int stepX = Math.Sign(dx);
            int stepY = Math.Sign(dy);
            int stepZ = Math.Sign(dz);

            double tDeltaX = dx != 0 ? Math.Abs(1.0 / dx) : double.PositiveInfinity;
            double tDeltaY = dy != 0 ? Math.Abs(1.0 / dy) : double.PositiveInfinity;
            double tDeltaZ = dz != 0 ? Math.Abs(1.0 / dz) : double.PositiveInfinity;

            // Start from voxel center (0.5, 0.5, 0.5), so first boundary is half a step away.
            double tMaxX = dx != 0 ? tDeltaX * 0.5 : double.PositiveInfinity;
            double tMaxY = dy != 0 ? tDeltaY * 0.5 : double.PositiveInfinity;
            double tMaxZ = dz != 0 ? tDeltaZ * 0.5 : double.PositiveInfinity;

            const double TieEpsilon = 1e-6;
            int maxSteps = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) + 2;

            for (int i = 0; i < maxSteps && (x != b.X || y != b.Y || z != b.Z); i++)
            {
                int prevX = x, prevY = y, prevZ = z;
                double minT = Math.Min(tMaxX, Math.Min(tMaxY, tMaxZ));

                bool advX = Math.Abs(tMaxX - minT) <= TieEpsilon && x != b.X;
                bool advY = Math.Abs(tMaxY - minT) <= TieEpsilon && y != b.Y;
                bool advZ = Math.Abs(tMaxZ - minT) <= TieEpsilon && z != b.Z;

                if (advX) { x += stepX; tMaxX += tDeltaX; }
                if (advY) { y += stepY; tMaxY += tDeltaY; }
                if (advZ) { z += stepZ; tMaxZ += tDeltaZ; }

                if (!IsStepClear(prevX, prevY, prevZ, x, y, z, preventCornerCutting, maxAllowedCost))
                    return false;
            }

            return x == b.X && y == b.Y && z == b.Z;
        }

        private bool IsStepClear(int px, int py, int pz, int nx, int ny, int nz, bool preventCornerCutting, byte maxAllowedCost)
        {
            byte cost = GetCost(nx, ny, nz);
            if (cost == BlockedCost || cost > maxAllowedCost)
                return false;

            if (!preventCornerCutting)
                return true;

            int dx = nx - px;
            int dy = ny - py;
            int dz = nz - pz;
            int axes = (dx != 0 ? 1 : 0) + (dy != 0 ? 1 : 0) + (dz != 0 ? 1 : 0);
            if (axes <= 1)
                return true;

            // Check axial neighbors for 2D/3D diagonals.
            if (dx != 0 && !IsCellAllowed(px + dx, py, pz, maxAllowedCost)) return false;
            if (dy != 0 && !IsCellAllowed(px, py + dy, pz, maxAllowedCost)) return false;
            if (dz != 0 && !IsCellAllowed(px, py, pz + dz, maxAllowedCost)) return false;

            // For 3-axis space diagonals, also verify all 3 face diagonals.
            if (axes == 3)
            {
                if (!IsCellAllowed(px + dx, py + dy, pz, maxAllowedCost)) return false;
                if (!IsCellAllowed(px + dx, py, pz + dz, maxAllowedCost)) return false;
                if (!IsCellAllowed(px, py + dy, pz + dz, maxAllowedCost)) return false;
            }

            return true;
        }

        private bool IsCellAllowed(int x, int y, int z, byte maxAllowedCost)
        {
            byte c = GetCost(x, y, z);
            return c != BlockedCost && c <= maxAllowedCost;
        }

        private void EnsureRegionsUpToDate()
        {
            if (_regionsVersion == Version && _regionIds != null)
                return;

            int count = _cells.Length;
            if (_regionIds == null || _regionIds.Length != count)
            {
                _regionIds = new int[count];
                _bfsQueue = new int[count];
            }
            else
            {
                Array.Clear(_regionIds, 0, count);
            }

            int nextRegionId = 0;
            int w = Width;
            int h = Height;
            int d = Depth;
            int wh = w * h;

            for (int i = 0; i < count; i++)
            {
                if (_cells[i] == BlockedCost || _regionIds[i] != 0)
                    continue;

                nextRegionId++;
                int head = 0;
                int tail = 0;
                _regionIds[i] = nextRegionId;
                _bfsQueue[tail++] = i;

                while (head < tail)
                {
                    int current = _bfsQueue[head++];
                    int x = current % w;
                    int rem = current / w;
                    int y = rem % h;
                    int z = rem / h;

                    if (x > 0) TryVisit(current - 1, nextRegionId, ref tail);
                    if (x + 1 < w) TryVisit(current + 1, nextRegionId, ref tail);
                    if (y > 0) TryVisit(current - w, nextRegionId, ref tail);
                    if (y + 1 < h) TryVisit(current + w, nextRegionId, ref tail);
                    if (z > 0) TryVisit(current - wh, nextRegionId, ref tail);
                    if (z + 1 < d) TryVisit(current + wh, nextRegionId, ref tail);
                }
            }

            _regionsVersion = Version;
        }

        private void TryVisit(int neighborIndex, int regionId, ref int tail)
        {
            if (_cells[neighborIndex] != BlockedCost && _regionIds[neighborIndex] == 0)
            {
                _regionIds[neighborIndex] = regionId;
                _bfsQueue[tail++] = neighborIndex;
            }
        }
    }
}
