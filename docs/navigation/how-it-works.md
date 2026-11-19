# How Navigation Works

`com.contentcontent.navigation` provides pure C# pathfinding and navigation algorithms decoupled from grid storage and Unity runtime types. It builds on top of `ContentContent.Core.Grid` (`IGrid<TCell>` and `GridCoord`) from `com.contentcontent.core`.

## Package Architecture

```text
Packages/com.contentcontent.navigation/
├── package.json                                  # Depends on com.contentcontent.core (1.0.0)
├── Runtime/
│   ├── ContentContent.Navigation.asmdef          # Pure C# (noEngineReferences: true), refs ContentContent.Core
│   └── GridPathfinder.cs                         # Generic A* pathfinder with binary min-heap
└── Tests/EditMode/
    ├── ContentContent.Navigation.Tests.asmdef    # NUnit EditMode test assembly
    └── GridPathfinderTests.cs                    # Obstacle, unreachable, and corner-cutting tests
```

Because `ContentContent.Navigation.asmdef` sets `"noEngineReferences": true`, pathfinding compiles without `UnityEngine`, runs deterministically on any cell type `TCell`, and executes in EditMode unit tests with zero scene setup.

---

## Separation from Core Grid

Grid storage (`GridCoord`, `IGrid<TCell>`, `Grid2D<TCell>`, `UnityGridTransform`, `GridBehaviour`) lives in `com.contentcontent.core` (`ContentContent.Core.Grid`), while pathfinding lives in `com.contentcontent.navigation` (`ContentContent.Navigation`):

- Projects that only need a 2D spatial grid (inventories, board games, tile placement, cellular automata) can depend on `com.contentcontent.core` alone without pulling in navigation algorithms.
- Projects that need pathfinding install `com.contentcontent.navigation`, which operates against the `IGrid<TCell>` interface so any custom grid implementation works out of the box.

---

## A* Pathfinder Design (`GridPathfinder`)

`GridPathfinder.TryFindPath<TCell>(...)` implements standard A* search over any `IGrid<TCell>`:

1. **Early Validation**:
   - Clears `outPath` immediately.
   - Returns `false` if `start` or `goal` is out of bounds or if `goal` is not walkable.
   - If `start == goal`, writes `[start]` to `outPath` and returns `true`.
2. **Admissible Heuristics**:
   - **4-way (`allowDiagonals: false`)**: Manhattan distance (`dx + dy`).
   - **8-way (`allowDiagonals: true`)**: Octile distance (`min(dx, dy) * √2 + |dx - dy|`).
3. **Priority Queue**:
   - Uses an internal binary `MinHeap` for `O(log n)` open-set pops with lazy duplicate skipping via a closed `HashSet<GridCoord>`.
4. **Custom Terrain Costs**:
   - Optional `getStepCost(coord, cell)` callback multiplies the base move cost (`1.0` for cardinal, `√2 ≈ 1.41421356` for diagonal). Costs are clamped to a minimum of `0.0001f` to preserve A* optimality.
5. **Diagonal Corner-Cutting Prevention**:
   - When `allowDiagonals: true` and `preventCornerCutting: true` (the default), a diagonal step `(dx, dy)` is only allowed if both orthogonal neighbors `(current.X + dx, current.Y)` and `(current.X, current.Y + dy)` are also walkable. This prevents agents from squeezing diagonally between two touching wall corners.
6. **Path Output**:
   - Populates the caller-supplied `List<GridCoord> outPath` in order from `start` to `goal` (inclusive), allowing callers to reuse a single list buffer across queries.
