using System;
using System.Collections.Generic;
using ContentContent.Core.Grid;
using ContentContent.Core.Grid.Unity;
using UnityEngine;

namespace ContentContent.Navigation.Unity
{
    /// <summary>
    /// Drop-in navigation volume supporting 2D (<see cref="NavMode.PlanarXY"/>),
    /// ground 2D (<see cref="NavMode.PlanarXZ"/>), and full volumetric 3D (<see cref="NavMode.Volumetric3D"/>).
    /// Automatically scans scene colliders on startup with no manual baking required.
    /// </summary>
    public class NavSurface : MonoBehaviour
    {
        private const int MaxGizmoBlockedCells = 4096;

        private static readonly List<NavSurface> s_activeSurfaces = new List<NavSurface>();
        public static IReadOnlyList<NavSurface> ActiveSurfaces => s_activeSurfaces;

        [SerializeField] private NavMode _mode = NavMode.PlanarXZ;
        [SerializeField] private Vector3 _size = new Vector3(20f, 10f, 20f);
        [SerializeField, Min(0.1f)] private float _cellSize = 1f;
        [SerializeField] private LayerMask _obstacleMask = ~0;
        [SerializeField, Min(0f)] private float _agentPadding = 0f;

        private NavGrid _grid;
        private readonly NavPathfinder _pathfinder = new NavPathfinder();
        private readonly NavPathQueue _pathQueue = new NavPathQueue();
        private readonly List<GridCoord3D> _coordScratch = new List<GridCoord3D>(64);

        public NavMode Mode => _mode;
        public Vector3 Size => _size;
        public float CellSize => Mathf.Max(0.1f, _cellSize);
        public LayerMask ObstacleMask => _obstacleMask;
        public NavGrid Grid => _grid;

        /// <summary>Maximum queued agent path requests processed per frame.</summary>
        public int MaxPathsPerFrame { get; set; } = 8;

        public Bounds WorldBounds => new Bounds(transform.position, GetEffectiveWorldSize());

        private void Awake()
        {
            BuildAndScan();
        }

        private void OnEnable()
        {
            if (!s_activeSurfaces.Contains(this))
                s_activeSurfaces.Add(this);
        }

        private void OnDisable()
        {
            s_activeSurfaces.Remove(this);
            _pathQueue.Clear();
        }

        private void Update()
        {
            if (_grid != null && _pathQueue.PendingCount > 0)
            {
                _pathQueue.ProcessBudget(_grid, _pathfinder, MaxPathsPerFrame);
            }
        }

        /// <summary>
        /// Rebuilds the underlying <see cref="NavGrid"/> and scans all cells for obstacles.
        /// </summary>
        [ContextMenu("Scan Now")]
        public void Scan()
        {
            BuildAndScan();
        }

        /// <summary>
        /// Re-scans only the cells intersecting <paramref name="worldBounds"/> for fast dynamic obstacle updates.
        /// </summary>
        public void RefreshBounds(Bounds worldBounds)
        {
            if (_grid == null)
            {
                BuildAndScan();
                return;
            }

            float pad = _agentPadding + CellSize * 0.5f;
            Vector3 minWorld = worldBounds.min - new Vector3(pad, pad, pad);
            Vector3 maxWorld = worldBounds.max + new Vector3(pad, pad, pad);

            GridCoord3D minCell = ClampToGrid(WorldToCell(minWorld));
            GridCoord3D maxCell = ClampToGrid(WorldToCell(maxWorld));

            ScanCellRange(minCell, maxCell);
        }

        /// <summary>
        /// Immediately computes a world-space path from <paramref name="startWorld"/> to <paramref name="goalWorld"/>.
        /// </summary>
        public bool TryFindPath(
            Vector3 startWorld,
            Vector3 goalWorld,
            List<Vector3> outWorldPath,
            bool allowDiagonals = true,
            bool smoothPath = true)
        {
            if (outWorldPath == null) throw new ArgumentNullException(nameof(outWorldPath));
            outWorldPath.Clear();

            if (_grid == null)
                BuildAndScan();

            GridCoord3D startCell = WorldToCell(startWorld);
            GridCoord3D goalCell = WorldToCell(goalWorld);

            if (!_grid.IsWalkable(startCell) && !_grid.TryFindNearestWalkable(startCell, 4, out startCell))
                return false;

            if (!_grid.IsWalkable(goalCell) && !_grid.TryFindNearestWalkable(goalCell, 4, out goalCell))
                return false;

            if (!_pathfinder.TryFindPath(_grid, startCell, goalCell, _coordScratch, allowDiagonals, preventCornerCutting: true, smoothPath))
                return false;

            ConvertCoordsToWorld(_coordScratch, outWorldPath, goalWorld);
            return true;
        }

        /// <summary>
        /// Enqueues a budgeted path request processed in <see cref="Update"/>.
        /// </summary>
        public void RequestPath(
            object owner,
            Vector3 startWorld,
            Vector3 goalWorld,
            Action<bool, List<Vector3>> callback,
            bool allowDiagonals = true,
            bool smoothPath = true)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (_grid == null)
                BuildAndScan();

            GridCoord3D startCell = WorldToCell(startWorld);
            GridCoord3D goalCell = WorldToCell(goalWorld);

            if (!_grid.IsWalkable(startCell))
                _grid.TryFindNearestWalkable(startCell, 4, out startCell);

            if (!_grid.IsWalkable(goalCell))
                _grid.TryFindNearestWalkable(goalCell, 4, out goalCell);

            Vector3 capturedGoal = goalWorld;
            _pathQueue.Enqueue(
                owner,
                startCell,
                goalCell,
                (found, coordPath) =>
                {
                    var worldPath = new List<Vector3>(coordPath.Count);
                    if (found)
                    {
                        ConvertCoordsToWorld(coordPath, worldPath, capturedGoal);
                    }
                    callback(found, worldPath);
                },
                allowDiagonals,
                preventCornerCutting: true,
                smoothPath);
        }

        public void CancelRequest(object owner) => _pathQueue.Cancel(owner);

        public bool IsWalkable(Vector3 worldPosition)
        {
            if (_grid == null)
                return false;
            return _grid.IsWalkable(WorldToCell(worldPosition));
        }

        public GridCoord3D WorldToCell(Vector3 worldPosition)
        {
            UnityGridTransform3D gt = GetGridTransform();
            GridCoord3D coord = gt.WorldToGrid(worldPosition);

            if (_mode == NavMode.PlanarXY)
                return new GridCoord3D(coord.X, coord.Y, 0);
            if (_mode == NavMode.PlanarXZ)
                return new GridCoord3D(coord.X, 0, coord.Z);

            return coord;
        }

        public Vector3 CellToWorld(GridCoord3D coord)
        {
            UnityGridTransform3D gt = GetGridTransform();
            Vector3 center = gt.GridToWorldCenter(coord);

            if (_mode == NavMode.PlanarXY)
                center.z = transform.position.z;
            else if (_mode == NavMode.PlanarXZ)
                center.y = transform.position.y;

            return center;
        }

        /// <summary>
        /// Finds the active <see cref="NavSurface"/> containing <paramref name="worldPosition"/>,
        /// or the closest active surface if none strictly contains it.
        /// </summary>
        public static NavSurface FindSurfaceForPoint(Vector3 worldPosition)
        {
            NavSurface closest = null;
            float closestSqrDist = float.MaxValue;

            for (int i = 0; i < s_activeSurfaces.Count; i++)
            {
                NavSurface surface = s_activeSurfaces[i];
                if (surface == null)
                    continue;

                Bounds bounds = surface.WorldBounds;
                if (bounds.Contains(worldPosition))
                    return surface;

                float sqrDist = bounds.SqrDistance(worldPosition);
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closest = surface;
                }
            }

            return closest;
        }

        private void BuildAndScan()
        {
            float cell = CellSize;
            int width = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(_size.x) / cell));
            int height = _mode == NavMode.PlanarXZ ? 1 : Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(_size.y) / cell));
            int depth = _mode == NavMode.PlanarXY ? 1 : Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(_size.z) / cell));

            _grid = new NavGrid(width, height, depth, _mode);
            ScanCellRange(GridCoord3D.Zero, new GridCoord3D(width - 1, height - 1, depth - 1));
        }

        private void ScanCellRange(GridCoord3D min, GridCoord3D max)
        {
            float cell = CellSize;
            float probeSize = Mathf.Max(0.05f, cell * 0.9f + _agentPadding * 2f);
            Vector3 halfExtents = new Vector3(probeSize * 0.5f, probeSize * 0.5f, probeSize * 0.5f);
            Vector2 boxSize2D = new Vector2(probeSize, probeSize);

            for (int z = min.Z; z <= max.Z; z++)
            {
                for (int y = min.Y; y <= max.Y; y++)
                {
                    for (int x = min.X; x <= max.X; x++)
                    {
                        Vector3 center = CellToWorld(new GridCoord3D(x, y, z));
                        bool blocked = CheckObstacleAt(center, halfExtents, boxSize2D);
                        _grid.SetWalkable(x, y, z, !blocked);
                    }
                }
            }
        }

        private bool CheckObstacleAt(Vector3 center, Vector3 halfExtents, Vector2 boxSize2D)
        {
            if (_mode == NavMode.PlanarXY)
            {
#if CONTENTCONTENT_PHYSICS2D
                Collider2D hit2D = Physics2D.OverlapBox(center, boxSize2D, 0f, _obstacleMask);
                if (hit2D != null && !hit2D.isTrigger)
                    return true;
#endif
#if CONTENTCONTENT_PHYSICS
                if (Physics.CheckBox(center, halfExtents, Quaternion.identity, _obstacleMask, QueryTriggerInteraction.Ignore))
                    return true;
#endif
                return false;
            }

#if CONTENTCONTENT_PHYSICS
            return Physics.CheckBox(center, halfExtents, Quaternion.identity, _obstacleMask, QueryTriggerInteraction.Ignore);
#else
            return false;
#endif
        }

        private void ConvertCoordsToWorld(List<GridCoord3D> coords, List<Vector3> outWorldPath, Vector3 exactGoalWorld)
        {
            outWorldPath.Clear();
            int count = coords.Count;
            for (int i = 0; i < count; i++)
            {
                outWorldPath.Add(CellToWorld(coords[i]));
            }

            // Snap final waypoint to the exact requested world goal if it lies in the goal cell.
            if (count > 0 && WorldToCell(exactGoalWorld) == coords[count - 1])
            {
                Vector3 snapped = exactGoalWorld;
                if (_mode == NavMode.PlanarXY)
                    snapped.z = transform.position.z;
                else if (_mode == NavMode.PlanarXZ)
                    snapped.y = transform.position.y;

                outWorldPath[count - 1] = snapped;
            }
        }

        private UnityGridTransform3D GetGridTransform()
        {
            Vector3 effective = GetEffectiveWorldSize();
            Vector3 origin = transform.position - effective * 0.5f;
            return new UnityGridTransform3D(origin, CellSize);
        }

        private Vector3 GetEffectiveWorldSize()
        {
            float cell = CellSize;
            float sx = Mathf.Max(cell, Mathf.Round(Mathf.Abs(_size.x) / cell) * cell);
            float sy = _mode == NavMode.PlanarXZ ? cell : Mathf.Max(cell, Mathf.Round(Mathf.Abs(_size.y) / cell) * cell);
            float sz = _mode == NavMode.PlanarXY ? cell : Mathf.Max(cell, Mathf.Round(Mathf.Abs(_size.z) / cell) * cell);
            return new Vector3(sx, sy, sz);
        }

        private GridCoord3D ClampToGrid(GridCoord3D coord)
        {
            return new GridCoord3D(
                Mathf.Clamp(coord.X, 0, _grid.Width - 1),
                Mathf.Clamp(coord.Y, 0, _grid.Height - 1),
                Mathf.Clamp(coord.Z, 0, _grid.Depth - 1));
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 effective = GetEffectiveWorldSize();
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position, effective);

            if (_grid == null)
                return;

            Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.35f);
            Vector3 cubeSize = Vector3.one * (CellSize * 0.85f);
            int drawn = 0;

            for (int z = 0; z < _grid.Depth && drawn < MaxGizmoBlockedCells; z++)
            {
                for (int y = 0; y < _grid.Height && drawn < MaxGizmoBlockedCells; y++)
                {
                    for (int x = 0; x < _grid.Width && drawn < MaxGizmoBlockedCells; x++)
                    {
                        if (!_grid.IsWalkable(x, y, z))
                        {
                            Gizmos.DrawCube(CellToWorld(new GridCoord3D(x, y, z)), cubeSize);
                            drawn++;
                        }
                    }
                }
            }
        }
    }
}
