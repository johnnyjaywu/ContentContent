# Navigation Scripting API

Complete reference for `ContentContent.Navigation` (pure C# core) and `ContentContent.Navigation.Unity` (drop-in Unity components).

---

## Namespace: `ContentContent.Navigation`

### `NavMode` (enum)

| Value | Description |
| --- | --- |
| `PlanarXY = 0` | 2D navigation on the `XY` plane (`Depth = 1`). |
| `PlanarXZ = 1` | 2D ground navigation on the `XZ` plane (`Height = 1`). |
| `Volumetric3D = 2` | Full 3D navigation across `X`, `Y`, and `Z`. |

---

### `NavGrid` (`sealed class`)

Compact 1-byte-per-cell navigation grid (`0` = blocked, `1..255` = walkable cost).

```csharp
public NavGrid(
    int width,
    int height,
    int depth,
    NavMode mode = NavMode.Volumetric3D,
    byte initialCost = DefaultWalkableCost)
```

| Member | Type / Return | Description |
| --- | --- | --- |
| `Mode` | `NavMode` | Active dimensionality/plane mode. |
| `Width`, `Height`, `Depth`, `Count` | `int` | Dimensions and total cell count. |
| `Version` | `int` | Incremented whenever any cell changes. |
| `IsInBounds(int x, int y, int z)` / `IsInBounds(GridCoord3D)` | `bool` | Bounds check. |
| `GetCost(...)` / `IsWalkable(...)` | `byte` / `bool` | Reads cell cost (`0` = blocked). |
| `SetWalkable(..., bool walkable)` / `SetCost(..., byte cost)` / `Fill(byte cost)` | `void` | Updates cells and increments `Version`. |
| `ToIndex(...)` / `FromIndex(int)` | `int` / `GridCoord3D` | Flat index conversions. |
| `AreConnected(GridCoord3D a, GridCoord3D b)` | `bool` | Lazy cardinal connected-region check (`O(1)` after build). |
| `TryFindNearestWalkable(GridCoord3D origin, int maxRadius, out GridCoord3D result)` | `bool` | Snaps to the closest walkable cell within `maxRadius`. |
| `HasLineOfSight(GridCoord3D a, GridCoord3D b, bool preventCornerCutting = true, byte maxAllowedCost = 255)` | `bool` | 3D DDA voxel visibility check. |

---

### `NavPathfinder` (`sealed class`)

Reusable zero-allocation A* pathfinder with generation-stamped scratch buffers and optional LOS path smoothing.

```csharp
public bool TryFindPath(
    NavGrid grid,
    GridCoord3D start,
    GridCoord3D goal,
    List<GridCoord3D> outPath,
    bool allowDiagonals = true,
    bool preventCornerCutting = true,
    bool smoothPath = true,
    int maxExpansions = 0)
```

| Parameter | Description |
| --- | --- |
| `grid` | `NavGrid` to search. |
| `start`, `goal` | Start and destination `GridCoord3D`. |
| `outPath` | Caller-owned list populated from `start` to `goal` (inclusive) on success. |
| `allowDiagonals` | Enables 8-way (planar) or 26-way (volumetric 3D) movement. |
| `preventCornerCutting` | Blocks diagonal steps that squeeze past blocked orthogonal/face neighbors. |
| `smoothPath` | Applies greedy LOS string-pulling to collapse stair-stepped segments. |
| `maxExpansions` | Optional search node budget (`0` = unlimited). |

---

### `NavPathQueue` (`sealed class`)

Per-frame budgeted path request scheduler with per-owner deduplication.

| Method | Return | Description |
| --- | --- | --- |
| `PendingCount` | `int` | Number of queued requests waiting to run. |
| `Enqueue(object owner, GridCoord3D start, GridCoord3D goal, Action<bool, List<GridCoord3D>> callback, ...)` | `void` | Enqueues or replaces an owner's pending path request. |
| `Cancel(object owner)` | `void` | Removes any pending request for `owner`. |
| `Clear()` | `void` | Clears all pending requests. |
| `ProcessBudget(NavGrid grid, NavPathfinder pathfinder, int maxRequests, int maxExpansionsPerPath = 0)` | `int` | Runs up to `maxRequests` queued queries and returns how many ran. |

---

## Namespace: `ContentContent.Navigation.Unity`

### `NavSurface` (`MonoBehaviour`)

Serialized fields: `_mode`, `_size`, `_cellSize`, `_obstacleMask`, `_agentPadding`.

| Member | Type / Return | Description |
| --- | --- | --- |
| `ActiveSurfaces` | `static IReadOnlyList<NavSurface>` | All currently enabled surfaces in the scene. |
| `FindSurfaceForPoint(Vector3 worldPosition)` | `static NavSurface` | Returns the surface containing `worldPosition` (or nearest active surface). |
| `Mode`, `Size`, `CellSize`, `ObstacleMask`, `Grid`, `WorldBounds` | properties | Surface configuration and backing `NavGrid`. |
| `MaxPathsPerFrame` | `int` | Per-frame budgeted request limit (default `8`). |
| `Scan()` | `void` | Rebuilds `NavGrid` and scans all cells. |
| `RefreshBounds(Bounds worldBounds)` | `void` | Re-scans only cells overlapping `worldBounds`. |
| `TryFindPath(Vector3 startWorld, Vector3 goalWorld, List<Vector3> outWorldPath, bool allowDiagonals = true, bool smoothPath = true)` | `bool` | Immediate world-space path query. |
| `RequestPath(object owner, Vector3 startWorld, Vector3 goalWorld, Action<bool, List<Vector3>> callback, ...)` | `void` | Enqueues a budgeted world-space path query. |
| `CancelRequest(object owner)` | `void` | Cancels a pending queued request. |
| `WorldToCell(Vector3)` / `CellToWorld(GridCoord3D)` / `IsWalkable(Vector3)` | conversion / query | Coordinate and walkability helpers. |

---

### `NavAgent` (`MonoBehaviour`)

Serialized fields: `_speed`, `_stoppingDistance`, `_rotateTowardsMovement`, `_repathInterval`, `_surface` (optional override).

| Member | Type / Return | Description |
| --- | --- | --- |
| `Surface`, `Speed`, `StoppingDistance`, `Destination`, `Velocity`, `IsStopped`, `HasPath`, `RemainingDistance` | properties | Agent state and configuration. |
| `OnDestinationReached` / `OnPathFailed` | `event Action` | C# lifecycle notifications. |
| `OnDestinationReachedUnityEvent` / `OnPathFailedUnityEvent` | `UnityEvent` | Code-free Inspector event hooks invoked alongside the C# events. |
| `SetSurface(NavSurface surface)` | `void` | Optional explicit surface binding (auto-discovered by default). |
| `SetDestination(Vector3 target)` | `void` | Requests a path and starts moving toward `target`. |
| `Stop()` | `void` | Halts movement and cancels any pending path request. |

---

### `NavFollowTarget` (`MonoBehaviour`)

Continuously updates a sibling `NavAgent` (`[RequireComponent(typeof(NavAgent))]`) toward a moving `Transform`.

| Member | Type / Return | Description |
| --- | --- | --- |
| `Target` | `Transform` | Transform to pursue (`null` stops the agent when `_stopWhenTargetLost` is true). |
| `UpdateInterval` | `float` | Minimum seconds between repath threshold checks. |
| `RepathDistanceThreshold` | `float` | Distance the target must move before queuing a new path. |
| `ForceRepath()` | `void` | Immediately evaluates target position and updates `NavAgent`. |

---

### `NavPatrol` (`MonoBehaviour`)

Patrols a sibling `NavAgent` across ordered scene waypoint `Transform`s in `PatrolMode.Loop` or `PatrolMode.PingPong`.

| Member | Type / Return | Description |
| --- | --- | --- |
| `Waypoints` | `IList<Transform>` | Ordered patrol transforms. |
| `Mode` | `PatrolMode` | `Loop` (wraps around) or `PingPong` (reverses at ends). |
| `WaitDuration` | `float` | Pause duration at each reached waypoint before advancing. |
| `CurrentWaypointIndex` | `int` | Active destination waypoint index. |
| `OnWaypointReached` | `UnityEvent<int>` | Fires the reached waypoint index on arrival. |
| `StartPatrol()` / `AdvanceToNextWaypoint()` | `void` | Starts patrolling from waypoint `0` or advances immediately. |

---

### `NavObstacle` (`MonoBehaviour`)

Serialized fields: `_padding`, `_fallbackSize`. Automatically refreshes overlapping `NavSurface` bounds on `OnEnable`, `OnDisable`, and transform movement.
