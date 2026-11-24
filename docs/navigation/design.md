# How Navigation Works

`com.contentcontent.navigation` is a zero-boilerplate navigation system for Unity that works seamlessly in **2D (`PlanarXY`)**, **ground 2D (`PlanarXZ`)**, and **full volumetric 3D (`Volumetric3D`)** — without Unity NavMesh's `XZ`-only restriction or mandatory manual baking.

## Package Architecture

```text
Packages/com.contentcontent.navigation/
├── package.json                                  # Depends on com.contentcontent.core (1.0.0)
├── Runtime/
│   ├── ContentContent.Navigation.asmdef          # Pure C# (noEngineReferences: true)
│   ├── NavMode.cs                                # PlanarXY, PlanarXZ, Volumetric3D
│   ├── NavGrid.cs                                # 1-byte cell storage, lazy regions, 3D LOS, snapping
│   ├── NavPathfinder.cs                          # Zero-GC flat-array A* + LOS string-pulling
│   ├── NavPathQueue.cs                           # Per-frame budgeted path request queue
│   └── Unity/
│       ├── ContentContent.Navigation.Unity.asmdef # Physics/Physics2D versionDefines
│       ├── NavSurface.cs                         # Auto-scanning 2D/3D volume + Gizmos
│       ├── NavAgent.cs                           # Auto-wiring agent (Rigidbody2D/Rigidbody/Transform)
│       └── NavObstacle.cs                        # Automatic dynamic obstacle carving
└── Tests/EditMode/
    ├── ContentContent.Navigation.Tests.asmdef
    ├── NavGridTests.cs
    ├── NavPathfinderTests.cs
    └── NavPathQueueTests.cs
```

Because `ContentContent.Navigation.asmdef` sets `"noEngineReferences": true`, all pathfinding, line-of-sight smoothing, region checks, and request budgeting compile without `UnityEngine` and run deterministically in EditMode tests. `ContentContent.Navigation.Unity.asmdef` uses `versionDefines` for `com.unity.modules.physics` and `com.unity.modules.physics2d`, so the package compiles cleanly in 2D-only, 3D-only, and hybrid projects.

---

## Core Concepts

### 1. Unified 2D & Volumetric 3D Grid (`NavGrid`)

`NavGrid` stores walkability and traversal cost in a compact **`byte[]`** (`0` = blocked, `1..255` = walkable cost multiplier; ~1 MB for 1M cells):

- **`PlanarXY`**: `Depth` is clamped to `1` (4/8-way movement in `X, Y`).
- **`PlanarXZ`**: `Height` is clamped to `1` (4/8-way movement in `X, Z`).
- **`Volumetric3D`**: Full `Width × Height × Depth` (6/26-way movement in `X, Y, Z`).
- **Lazy Connected Regions (`AreConnected`)**: Maintains a `Version` counter and computes cardinal connected-component IDs on demand via iterative BFS. Unreachable goals fail in `O(1)` instead of flooding the entire volume.
- **3D Supercover Line of Sight (`HasLineOfSight`)**: Amanatides-Woo DDA voxel traversal with corner-cutting and terrain-cost checks.
- **Walkable Snapping (`TryFindNearestWalkable`)**: Expanding-shell search that snaps slightly blocked or out-of-bounds positions to the closest valid cell.

### 2. Zero-GC Flat-Array A* & Path Smoothing (`NavPathfinder`)

`NavPathfinder` is a reusable instance backed by flat arrays (`_gScore[]`, `_cameFrom[]`, `_closedStamp[]`, `_seenStamp[]`) and an `int`-indexed binary min-heap:

- **Generation Stamps**: Incrementing `_searchId` resets state in `O(1)` with zero heap allocations per query.
- **Admissible Heuristics**: Manhattan for cardinal movement, 2D Octile for 8-way planar, and 3D Octile (`d0√3 + (d1 - d0)√2 + (d2 - d1)`) for 26-way volumetric 3D.
- **Corner-Cutting Prevention**: Diagonal steps in 2D require both adjacent cardinal cells to be walkable; 3D space diagonals require all 3 axial and 3 face-diagonal neighbors to be walkable.
- **Line-of-Sight String-Pulling**: When `smoothPath: true`, greedy forward LOS checks collapse stair-stepped grid paths into minimal straight segments while respecting obstacles and terrain costs.

### 3. Drop-In Unity Workflow (`NavSurface`, `NavAgent`, `NavObstacle`)

1. **`NavSurface`**: Drop in the scene, choose `Mode` (`PlanarXY`, `PlanarXZ`, `Volumetric3D`), set `Size`, `CellSize`, and `ObstacleMask`. Automatically scans colliders on `Awake` (no manual bake step) and processes agent requests through a per-frame `NavPathQueue` budget (`MaxPathsPerFrame = 8`).
2. **`NavAgent`**: Drop on any character, set `Speed`, and call `SetDestination(target)`. Auto-discovers the containing `NavSurface`, auto-detects `Rigidbody2D` / `Rigidbody` / `Transform`, and automatically repaths when `NavSurface.Grid.Version` changes.
3. **`NavObstacle`**: Drop on moving doors, crates, or dynamic walls. Automatically calls `NavSurface.RefreshBounds(...)` on enable, disable, and movement so only affected cells are re-scanned.
