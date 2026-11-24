# Navigation Tutorial

This tutorial walks through installing `com.contentcontent.navigation`, setting up zero-boilerplate 2D or volumetric 3D navigation in Unity in under a minute, adding dynamic obstacles, and using the pure-C# API directly.

---

## Step 1: Install the Package

`com.contentcontent.navigation` depends on `com.contentcontent.core`. Install both via **Window → Package Manager → + → Add package from git URL...**:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.navigation
```

If you are working inside the `ContentContent` repository directly, both packages are already present under `Packages/`.

### Try the Built-In Sample

1. Ensure `com.contentcontent.core` is installed, open **Window → Package Manager**, select **ContentContent - Navigation**, and click **Download** on **Navigation Playground** under **Downloadable Samples**.
2. Click **Open Scene** (or open `Assets/Samples/ContentContent/Navigation/NavPlayground/NavPlayground.unity`) and press **Play** to watch `NavSurface` and `NavAgent` navigate around static walls and a toggleable dynamic doorway gate.

---

## Step 2: 60-Second Unity Setup (2D `XY`, Ground `XZ`, or Volumetric `3D`)

1. **Create a `NavSurface`**:
   - Create an empty GameObject and add **`NavSurface`**.
   - Pick **`Mode`**:
     - `PlanarXY` for 2D games (scans `Collider2D` and 3D `Collider` obstacles).
     - `PlanarXZ` for 3D ground/top-down games (scans 3D `Collider` obstacles).
     - `Volumetric3D` for flying/underwater 3D games (scans 3D `Collider` obstacles across `X, Y, Z`).
   - Set **`Size`** (world bounds centered on the GameObject), **`Cell Size`**, and **`Obstacle Mask`**.
   - *No baking step is required* — `NavSurface` scans automatically on `Awake`.
2. **Add a `NavAgent`**:
   - Add **`NavAgent`** to any character (works with `Rigidbody2D`, `Rigidbody`, or plain `Transform`).
   - Set **`Speed`** and **`Stopping Distance`**. No need to assign the `NavSurface` — `NavAgent` finds it automatically.
3. **Move the Agent**:

```csharp
using ContentContent.Navigation.Unity;
using UnityEngine;

public class SimpleClickToMove : MonoBehaviour
{
    [SerializeField] private NavAgent _agent;

    public void MoveTo(Vector3 targetWorldPosition)
    {
        _agent.SetDestination(targetWorldPosition);
    }
}
```

---

## Step 3: Dynamic Obstacles (`NavObstacle`)

Add **`NavObstacle`** to any moving door, crate, or destructible wall that has a `Collider` or `Collider2D`:

- When enabled, disabled, or moved, `NavObstacle` automatically calls `NavSurface.RefreshBounds(...)` on only the cells intersecting its old and new bounds.
- Any `NavAgent` currently navigating that surface sees `Grid.Version` change and automatically repaths around the obstacle.

---

## Step 4: Pure C# Volumetric 3D or 2D Pathfinding

For headless simulation, server logic, or custom movement controllers, use `NavGrid` and `NavPathfinder` directly with zero `UnityEngine` dependencies:

```csharp
using System.Collections.Generic;
using ContentContent.Core.Grid;
using ContentContent.Navigation;

// Volumetric 3D space (16 x 16 x 16)
var navGrid = new NavGrid(16, 16, 16, NavMode.Volumetric3D);
navGrid.SetWalkable(5, 5, 5, false); // obstacle voxel
navGrid.SetCost(6, 5, 5, 3);         // slower terrain (3x cost)

// Reuse one NavPathfinder instance for zero-GC queries
var pathfinder = new NavPathfinder();
var path = new List<GridCoord3D>();

bool found = pathfinder.TryFindPath(
    grid: navGrid,
    start: new GridCoord3D(0, 0, 0),
    goal: new GridCoord3D(15, 12, 15),
    outPath: path,
    allowDiagonals: true,
    preventCornerCutting: true,
    smoothPath: true);
```

---

## Step 5: Run EditMode Tests

1. Open **Window → General → Test Runner**.
2. Switch to the **EditMode** tab.
3. Expand **ContentContent.Navigation.Tests** and click **Run All** to verify `NavGridTests`, `NavPathfinderTests`, and `NavPathQueueTests`.
