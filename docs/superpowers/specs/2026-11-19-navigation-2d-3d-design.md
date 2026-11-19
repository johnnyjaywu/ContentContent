# Design: Unified 2D (XY/XZ) & Volumetric 3D Navigation System

**Date:** 2026-11-19  
**Packages affected:** `com.contentcontent.core`, `com.contentcontent.navigation`

---

## 1. Intent & Why

Unity's built-in NavMesh is locked to the `XZ` plane, requires manual baking workflows, and does not support 2D (`XY`) or volumetric 3D (`XYZ`) navigation out of the box.

This upgrade turns `com.contentcontent.navigation` into a drop-in navigation system that works seamlessly across:
- **2D (`PlanarXY`)** — top-down or side-view 2D games using `Physics2D` colliders.
- **2D Ground (`PlanarXZ`)** — top-down / isometric / strategy 3D games on the ground plane using 3D `Physics` colliders.
- **Full Volumetric 3D (`Volumetric3D`)** — flying, swimming, or zero-G 3D navigation in `(X, Y, Z)` using 3D `Physics` colliders.

The top priority is **simple, zero-boilerplate setup** over deep customizability: drop a `NavSurface` in the scene, drop a `NavAgent` on a character, and call `agent.SetDestination(target)`.

---

## 2. Goals, Non-Goals & Anti-Goals

### Goals
- **3-component Unity workflow** (`NavSurface`, `NavAgent`, `NavObstacle`) with auto-discovery (no manual Inspector wiring between agents and surfaces) and automatic scanning on startup (no mandatory bake step).
- **Unified pure-C# engine** (`noEngineReferences: true`) supporting 2D slices (`XY`, `XZ`) and full 3D volumes (`XYZ`) with one algorithm and full EditMode NUnit test coverage.
- **Smooth, natural paths** via line-of-sight string-pulling so agents don't stair-step along grid centers.
- **Large-scene performance built in** without adding setup complexity:
  1. Zero-allocation flat-array A* with generation stamps.
  2. Compact 1-byte cell storage (~1 MB for 1M cells).
  3. Bounds-scoped partial re-scans for moving/toggling obstacles.
  4. Lazy connected-region IDs for `O(1)` unreachable rejection + optional expansion cap.
  5. Per-frame budgeted path queue so many agents repathing simultaneously don't spike frames.
- **Backwards compatibility** with existing `Grid2D<TCell>`, `GridCoord`, and `GridPathfinder`.

### Non-Goals
- Kilometer-scale open-world streaming (sparse `16³` chunks or HPA* can be swapped under `NavSurface` later without changing the public API).
- Off-mesh links, jump pads, or platformer gravity arcs.
- Local crowd avoidance (RVO/ORCA) or multiple baked agent-radius layers.

### Anti-Goals
- Inspector bloat with dozens of obscure tuning parameters.
- Requiring a manual Editor bake before Play Mode works.
- Leaking `UnityEngine` dependencies into `ContentContent.Core` or `ContentContent.Navigation`.

---

## 3. Constraints

- Keep `"noEngineReferences": true` on `ContentContent.Core.asmdef` and `ContentContent.Navigation.asmdef`.
- Target Unity 2022.3+ / C# 9 (.NET Standard 2.1), verified in this repo's Unity `7000.0.0a16` batchmode compile & EditMode test runner.
- Follow existing repo conventions: pure C# in `Runtime/`, thin Unity adapters in `Runtime/Unity/`, EditMode tests in `Tests/EditMode/`, and Docsify docs in `docs/`.

---

## 4. Approaches Considered

| Approach | Summary | Trade-offs | Decision |
| --- | --- | --- | --- |
| **A. Unified Grid Core + 3 Drop-In Unity Components** | `Grid3D` + 1-cell-thick 2D slices, compact `byte[]` `NavGrid`, zero-GC flat-array A* + LOS string-pulling, `NavSurface` / `NavAgent` / `NavObstacle`. | Dead-simple setup, instant dynamic obstacles, single unified algorithm for 2D & 3D, scales to ~`100³` easily (~1 MB). Not for km-scale open space without future chunking. | **Selected** |
| **B. Sparse Voxel Octree (3D) + `Grid2D` (2D)** | Separate octree for 3D and 2D grid for planar. | Lower memory for huge empty 3D worlds, but two engines to maintain and heavier dynamic updates. | Rejected |
| **C. Auto-Sampled Visibility / Waypoint Graph** | Sample free space into a 2D/3D visibility graph. | Fast runtime queries, but fragile 3D generation, slow dynamic updates, and requires tuning sampling density. | Rejected |

---

## 5. Architecture & API Design

### 5.1 Core 3D Grid (`Packages/com.contentcontent.core`)

Added alongside existing `Grid2D<TCell>` and `GridCoord`:

- **`ContentContent.Core.Grid.GridCoord3D`** (`readonly struct`, `IEquatable<GridCoord3D>`):
  - `X`, `Y`, `Z` (`int`), `SqrMagnitude`, `ManhattanDistance`, `ChebyshevDistance`.
  - Constants: `Zero`, `Right`, `Left`, `Up`, `Down`, `Forward`, `Back`.
  - Pre-allocated direction arrays: `CardinalDirections` (6-way) and `AllDirections` (26-way).
  - Operators: `+`, `-`, `* int`, `==`, `!=`.
- **`ContentContent.Core.Grid.IGrid3D<TCell>` & `Grid3D<TCell>`**:
  - Flat 1D array backed (`x + Width * (y + Height * z)`).
  - `Width`, `Height`, `Depth`, `Count`, indexers `[x, y, z]` and `[GridCoord3D]`, `OnCellChanged`, `IsInBounds`, `TryGet`, `TrySet`, `Fill`, `GetNeighbors(coord, results, includeDiagonals)`, `ToIndex`, `FromIndex`.
- **`ContentContent.Core.Grid.Unity.UnityGridTransform3D`** (`[Serializable] struct`):
  - `origin` (`Vector3`), `cellSize` (`float`).
  - `GridToWorldCorner(GridCoord3D)`, `GridToWorldCenter(GridCoord3D)`, `WorldToGrid(Vector3)`, `ToGridCoord3D(Vector3Int)`, `ToVector3Int(GridCoord3D)`.

### 5.2 Pure-C# Navigation Core (`Packages/com.contentcontent.navigation/Runtime/`)

Existing `GridPathfinder` stays intact for backwards compatibility. New unified types:

- **`NavMode` (enum)**:
  - `PlanarXY` — 2D navigation in `X, Y` (`Depth == 1`).
  - `PlanarXZ` — 2D navigation in `X, Z` (`Height == 1`).
  - `Volumetric3D` — Full 3D navigation in `X, Y, Z`.

- **`NavGrid` (class)**:
  - Compact `byte[] _cells` (`0` = blocked, `1..255` = walkable cost where `1` = normal cost; default initialized to `1`).
  - Dimensions: `Width`, `Height`, `Depth`, `Count`, `Mode`.
  - `int Version`: incremented whenever any cell changes (used by `NavAgent` to detect invalidated paths and by `NavGrid` to mark connected regions dirty).
  - Methods:
    - `IsInBounds(int x, int y, int z)` / `IsInBounds(GridCoord3D)`
    - `IsWalkable(GridCoord3D)` / `GetCost(GridCoord3D)`
    - `SetWalkable(GridCoord3D, bool walkable)` / `SetCost(GridCoord3D, byte cost)` / `Fill(byte cost)`
    - `ToIndex(int x, int y, int z)` / `FromIndex(int index)`
    - `AreConnected(GridCoord3D a, GridCoord3D b)`: lazily computes cardinal connected-component `int[] _regionIds` via iterative BFS flood-fill whenever `Version != _regionsVersion`, giving `O(1)` unreachable rejection on subsequent queries (used whenever `!allowDiagonals || preventCornerCutting`, since corner-safe diagonal paths are always cardinally connected).
    - `HasLineOfSight(GridCoord3D a, GridCoord3D b, bool preventCornerCutting = true)`: 3D supercover DDA voxel traversal checking walkability along the straight segment (used by string-pulling).
    - `TryFindNearestWalkable(GridCoord3D origin, int maxRadius, out GridCoord3D result)`: expanding-shell search to snap out-of-bounds or slightly-blocked positions to the closest valid cell.

- **`NavPathfinder` (class — reusable instance with zero-GC scratch buffers)**:
  - Holds flat arrays sized to the grid (`float[] _gScore`, `int[] _cameFrom`, `int[] _closedStamp`, `int[] _seenStamp`, `int _searchId`, plus an `int`-indexed binary `MinHeap`). Automatically resizes only if used on a larger `NavGrid`.
  - `TryFindPath(NavGrid grid, GridCoord3D start, GridCoord3D goal, List<GridCoord3D> outPath, bool allowDiagonals = true, bool preventCornerCutting = true, bool smoothPath = true, int maxExpansions = 0)`:
    - Clamps/validates bounds & walkability; returns `false` immediately if `!grid.AreConnected(start, goal)` (when using default connectivity).
    - Admissible heuristics:
      - 4/6-way (`!allowDiagonals`): Manhattan (`dx + dy + dz`).
      - 2D 8-way: Octile (`min * √2 + (max - min)`).
      - 3D 26-way: 3D Octile (`dMin * √3 + (dMid - dMin) * √2 + (dMax - dMid)`).
    - Corner-cutting prevention:
      - 2D diagonal `(dx, dy)`: requires both `(x+dx, y)` and `(x, y+dy)` walkable.
      - 3D diagonal `(dx, dy, dz)`: requires all axial neighbors along the non-zero axes (and face diagonals for 3-axis corners) to be walkable so agents never clip through edges or corners.
    - Optional `smoothPath = true`: greedy line-of-sight string-pulling over the reconstructed path using `grid.HasLineOfSight`, keeping only waypoints where LOS breaks (preserving `start` and `goal`).

- **`NavPathQueue` (class)**:
  - Lightweight pure-C# FIFO request queue with a configurable `MaxRequestsPerStep` budget (default `8`) so `NavSurface` can spread agent repaths across frames without frame spikes. Duplicate pending requests for the same owner overwrite the older request in place.

### 5.3 Unity Adapters (`Packages/com.contentcontent.navigation/Runtime/Unity/`)

New assembly `ContentContent.Navigation.Unity.asmdef` referencing `ContentContent.Core`, `ContentContent.Core.Unity`, and `ContentContent.Navigation`, with `versionDefines` for `com.unity.modules.physics` (`CONTENTCONTENT_PHYSICS`) and `com.unity.modules.physics2d` (`CONTENTCONTENT_PHYSICS2D`) so the package compiles cleanly in 2D-only, 3D-only, and hybrid Unity projects.

1. **`NavSurface` (`MonoBehaviour`)**:
   - **Minimal Inspector** (5 fields):
     - `Mode` (`NavMode`, default `PlanarXZ`)
     - `Size` (`Vector3`, default `(20, 10, 20)` — unused axis auto-clamped to 1 cell for planar modes)
     - `CellSize` (`float`, default `1f`)
     - `ObstacleMask` (`LayerMask`, default `~0`)
     - `AgentPadding` (`float`, default `0f` — expands obstacle overlap box for clearance)
   - **Zero-bake startup**: Automatically builds `NavGrid` and scans colliders in `Awake()`.
     - `PlanarXY`: uses `Physics2D.OverlapBox(center, boxSize, 0f, ObstacleMask)` (ignoring triggers).
     - `PlanarXZ` / `Volumetric3D`: uses `Physics.CheckBox(center, halfExtents, Quaternion.identity, ObstacleMask, QueryTriggerInteraction.Ignore)`.
   - **Dynamic updates**: `Scan()` (full) and `RefreshBounds(Bounds worldBounds)` (partial, only re-scans overlapping cells).
   - **Auto-discovery**: Maintains `IReadOnlyList<NavSurface> ActiveSurfaces` and `FindSurfaceForPoint(Vector3)` so `NavAgent` and `NavObstacle` find their surface automatically.
   - **Budgeted pathing**: Processes queued `NavAgent` requests in `Update()` via `NavPathQueue` (`_maxPathsPerFrame = 8`).
   - **Immediate query API**: `TryFindPath(Vector3 start, Vector3 goal, List<Vector3> outWorldPath)`, `WorldToCell(Vector3)`, `CellToWorld(GridCoord3D)`, `IsWalkable(Vector3)`.
   - **Gizmos**: Draws volume bounds wireframe and blocked cells when selected in Scene view.

2. **`NavAgent` (`MonoBehaviour`)**:
   - **Minimal Inspector** (4 fields):
     - `Speed` (`float`, default `5f`)
     - `StoppingDistance` (`float`, default `0.15f`)
     - `RotateTowardsMovement` (`bool`, default `true`)
     - `RepathInterval` (`float`, default `0.25f`)
   - **Zero wiring**: Auto-binds to the `NavSurface` containing its position on `OnEnable()` (optional `SetSurface(NavSurface)` override available in code).
   - **Auto-movement**: Automatically uses `Rigidbody2D.MovePosition` (in `PlanarXY`), `Rigidbody.MovePosition` (in `PlanarXZ`/`Volumetric3D`), or `transform.position` if no Rigidbody is attached.
   - **Simple API**:
     - `SetDestination(Vector3 target)`
     - `Stop()`
     - Properties: `HasPath`, `IsStopped`, `Destination`, `RemainingDistance`, `Velocity`
     - Events: `OnDestinationReached`, `OnPathFailed`
   - **Smart repathing**: Automatically queues a repath if `NavSurface.Grid.Version` changes while moving, or if `SetDestination` moves the target by more than `0.5 * CellSize` (throttled by `RepathInterval`).
   - **Gizmos**: Draws active path waypoints in Scene view when selected.

3. **`NavObstacle` (`MonoBehaviour`)**:
   - **Minimal Inspector**: optional `Padding` (`float`, default `0f`) and `FallbackSize` (`Vector3`, default `(1, 1, 1)` used only if no `Collider` or `Collider2D` is attached).
   - Automatically calls `NavSurface.RefreshBounds(...)` on `OnEnable()`, `OnDisable()`, and whenever the object's world bounds move beyond a small epsilon in `Update()` — zero code needed for moving doors, crates, or destructible walls.

---

## 6. Decisions Left to Implementation

- Exact `MinHeap` backing structure (flat `int[]` / `float[]` arrays for zero boxing/tuple overhead).
- Exact gizmo colors and cell-preview cap (cap gizmo cube drawing on huge volumes so Scene view stays smooth).
- Documentation updates in `docs/core/` and `docs/navigation/` once implemented.
