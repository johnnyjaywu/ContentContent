# Navigation Tutorial

This tutorial walks through installing `com.contentcontent.navigation`, running 4-way and 8-way A* pathfinding over `Grid2D<TCell>`, applying custom terrain costs, and converting paths to Unity world positions.

---

## Step 1: Install the Package

`com.contentcontent.navigation` depends on `com.contentcontent.core`. Install both packages via **Window → Package Manager → + → Add package from git URL...**:

```text
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.navigation
```

If you are working inside the `GameSystems` repository directly, both packages are already present under `Packages/`.

---

## Step 2: Basic A* Pathfinding

```csharp
using System.Collections.Generic;
using ContentContent.Core.Grid;
using ContentContent.Navigation;

var grid = new Grid2D<bool>(16, 16, _ => true);

// Place an obstacle wall
for (int y = 0; y < 12; y++)
{
    grid[5, y] = false;
}

var path = new List<GridCoord>();
bool found = GridPathfinder.TryFindPath(
    grid: grid,
    start: new GridCoord(0, 0),
    goal: new GridCoord(10, 0),
    isWalkable: (_, walkable) => walkable,
    outPath: path,
    allowDiagonals: true,
    preventCornerCutting: true);
```

When `found` is `true`, `path[0]` is `start` and `path[path.Count - 1]` is `goal`. When `found` is `false`, `path` is empty.

---

## Step 3: Custom Terrain Costs

Because `Grid2D<TCell>` and `GridPathfinder` are generic over `TCell`, you can store rich tile data and supply a `getStepCost` delegate:

```csharp
using System.Collections.Generic;
using ContentContent.Core.Grid;
using ContentContent.Navigation;

public enum TerrainType { Road, Grass, Mud, Water }

var map = new Grid2D<TerrainType>(20, 20, _ => TerrainType.Grass);
map[3, 3] = TerrainType.Water;
map[4, 3] = TerrainType.Mud;

var path = new List<GridCoord>();
bool found = GridPathfinder.TryFindPath(
    grid: map,
    start: new GridCoord(0, 0),
    goal: new GridCoord(10, 10),
    isWalkable: (_, terrain) => terrain != TerrainType.Water,
    outPath: path,
    getStepCost: (_, terrain) => terrain switch
    {
        TerrainType.Road => 0.5f,
        TerrainType.Mud => 3.0f,
        _ => 1.0f
    },
    allowDiagonals: true);
```

---

## Step 4: Using with `GridBehaviour` in Unity

Combine `GridBehaviour` (from `ContentContent.Core.Grid.Unity`) with `GridPathfinder` to convert world clicks into world-space waypoints:

```csharp
using System.Collections.Generic;
using ContentContent.Core.Grid;
using ContentContent.Core.Grid.Unity;
using ContentContent.Navigation;
using UnityEngine;

public class GridNavigator : MonoBehaviour
{
    [SerializeField] private GridBehaviour _gridBehaviour;

    private Grid2D<bool> _walkableGrid;
    private readonly List<GridCoord> _coordBuffer = new List<GridCoord>();

    private void Awake()
    {
        _walkableGrid = _gridBehaviour.CreateGrid<bool>(_ => true);
    }

    public bool TryComputeWorldPath(Vector3 startWorld, Vector3 goalWorld, List<Vector3> outWorldWaypoints)
    {
        outWorldWaypoints.Clear();

        GridCoord start = _gridBehaviour.WorldToGrid(startWorld);
        GridCoord goal = _gridBehaviour.WorldToGrid(goalWorld);

        if (!GridPathfinder.TryFindPath(
                _walkableGrid,
                start,
                goal,
                isWalkable: (_, walkable) => walkable,
                outPath: _coordBuffer,
                allowDiagonals: true,
                preventCornerCutting: true))
        {
            return false;
        }

        for (int i = 0; i < _coordBuffer.Count; i++)
        {
            outWorldWaypoints.Add(_gridBehaviour.GridToWorldCenter(_coordBuffer[i]));
        }

        return true;
    }
}
```

---

## Step 5: Run EditMode Tests

1. Open **Window → General → Test Runner**.
2. Switch to the **EditMode** tab.
3. Expand **ContentContent.Navigation.Tests** and click **Run All** to verify `GridPathfinderTests`.
