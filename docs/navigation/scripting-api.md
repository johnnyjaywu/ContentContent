# Navigation Scripting API

Complete reference for `ContentContent.Navigation` (`com.contentcontent.navigation`).

---

## Namespace: `ContentContent.Navigation`

### `GridPathfinder` (`public static class`)

Generic A* pathfinder operating on any `IGrid<TCell>` from `ContentContent.Core.Grid`.

#### `TryFindPath<TCell>`

```csharp
public static bool TryFindPath<TCell>(
    IGrid<TCell> grid,
    GridCoord start,
    GridCoord goal,
    Func<GridCoord, TCell, bool> isWalkable,
    List<GridCoord> outPath,
    Func<GridCoord, TCell, float> getStepCost = null,
    bool allowDiagonals = false,
    bool preventCornerCutting = true)
```

Attempts to find a shortest path from `start` to `goal`. Always clears `outPath` before running. When a path is found, populates `outPath` from `start` to `goal` (inclusive) and returns `true`; otherwise leaves `outPath` empty and returns `false`.

#### Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `grid` | `IGrid<TCell>` | *(required)* | Bounded 2D grid to search. Throws `ArgumentNullException` if `null`. |
| `start` | `GridCoord` | *(required)* | Starting grid coordinate. Returns `false` if out of bounds. |
| `goal` | `GridCoord` | *(required)* | Destination grid coordinate. Returns `false` if out of bounds or not walkable. |
| `isWalkable` | `Func<GridCoord, TCell, bool>` | *(required)* | Predicate returning `true` for traversable cells. Throws `ArgumentNullException` if `null`. |
| `outPath` | `List<GridCoord>` | *(required)* | Caller-owned list cleared and populated with the resulting path. Throws `ArgumentNullException` if `null`. |
| `getStepCost` | `Func<GridCoord, TCell, float>` | `null` | Optional per-cell terrain multiplier (clamped to `>= 0.0001f`). Defaults to `1f` when `null`. |
| `allowDiagonals` | `bool` | `false` | Enables 8-way movement (`√2` base diagonal cost and Octile heuristic) instead of 4-way cardinal movement (Manhattan heuristic). |
| `preventCornerCutting` | `bool` | `true` | When `allowDiagonals` is `true`, blocks diagonal steps unless both adjacent cardinal cells are also walkable. |

#### Return Value

`bool` — `true` if a valid path from `start` to `goal` was found; `false` otherwise.
