# Navigation Design & Best Practices

Built-in engine NavMeshes excel at static 3D triangle terrain, but break down for **2D tilemaps**, **dynamic building/digging games**, or **full 3D swimming and flying AI**. Beginners often react by either hacking transform steering that gets stuck on corners or running fresh unbudgeted A* allocations every frame per enemy. `com.contentcontent.navigation` solves all three modes (`PlanarXY`, `PlanarXZ`, `Volumetric3D`) with one compact, zero-GC architecture.

---

## 1. Compact Grid + Budgeted Scheduling

```text
   Scene Colliders ──(NavSurface / NavObstacle)──► 1-Byte NavGrid (0 = blocked, 1..255 = cost)
                                                          │
   NavAgent ──(RequestPath)──► NavPathQueue ──(N/frame)──► NavPathfinder (Zero-GC A* + LOS Smooth)
```

### Why This Design Works
- **1 byte per cell:** A 64×64×16 volumetric 3D grid uses ~64 KB of flat managed memory with zero per-node reference allocations.
- **Generation-stamped search buffers:** `NavPathfinder` never clears giant arrays with `Array.Clear` between queries; it increments a single `int` search generation counter, making queries deterministic and GC-free.
- **Line-of-sight string pulling:** After finding a grid path, a 3D DDA voxel visibility pass strips unnecessary zig-zags so agents walk smoothly instead of snapping cell-by-cell.
- **Frame-rate stability via `NavPathQueue`:** When 40 agents all chase the player through a changing door, `NavPathQueue` deduplicates per-owner requests and solves at most `MaxPathsPerFrame` queries per tick instead of spiking frame time.

---

## 2. Best Practices & Common Pitfalls

- **Match `CellSize` to your smallest hallway, not smaller:** Cutting `CellSize` in half multiplies 2D cells by $4\times$ and 3D cells by $8\times$. Aim for cells around $0.5\times$ to $1.0\times$ character width and add clearance via `Agent Padding`.
- **Keep player layers out of `ObstacleMask`:** If an enemy's own collider matches `ObstacleMask`, `NavSurface` will mark the agent's starting tile as solid. Always put walls/obstacles on dedicated layers.
- **Prefer `NavFollowTarget` over calling `SetDestination` in every `Update`:** `NavFollowTarget` only queues a new path when the target actually shifts past `RepathDistanceThreshold`.

---

## 3. Engine Transferability (Godot & Unreal)

- **Godot:** Layer 1 (`ContentContent.Navigation`) runs unchanged in Godot C#, mirroring `AStarGrid2D` / `AStar3D` while adding native `NavPathQueue` per-frame budgeting and 3D LOS smoothing.
- **Unreal:** Comparable to a uniform sparse grid / voxel SVO plugin for flying AI paired with UE's async pathfinding queue and runtime dynamic obstacle invalidation (`NavModifierVolume`).
