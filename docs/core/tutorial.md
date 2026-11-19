# Core Package Tutorial

This tutorial walks through installing `com.contentcontent.core` and using each of its three sub-domains: **Object Pooling**, **Hierarchical State Machine (HFSM)**, and **2D Grid**.

---

## Step 1: Install the Package

Open **Window → Package Manager → + → Add package from git URL...** and enter:

```text
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.core
```

If you are working inside the `GameSystems` repository directly, the package is already present at `Packages/com.contentcontent.core`.

---

## Step 2: Object Pooling

### Pure C# Scoped Pooling

Use `ObjectPool<T>` with `PooledHandle<T>` (`using` scope) or manual `Get()` / `Release()`:

```csharp
using ContentContent.Core.Pooling;

public class ProjectileData : IPoolable
{
    public float Speed;
    public void OnSpawned() => Speed = 10f;
    public void OnDespawned() => Speed = 0f;
}

var pool = new ObjectPool<ProjectileData>(
    createFunc: () => new ProjectileData(),
    defaultCapacity: 16,
    maxSize: 128);

pool.Prewarm(16);

// Automatically released back to the pool at the end of the scope
using (pool.Get(out var data))
{
    data.Speed = 25f;
}
```

### Pooling Unity Prefabs

Attach `PrefabPoolBehaviour` to a scene object, assign a prefab (optionally with a `PooledObject` component on it), and call `Spawn`:

```csharp
using ContentContent.Core.Pooling.Unity;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private PrefabPoolBehaviour _bulletPool;

    public void Fire(Vector3 muzzlePos, Quaternion rotation)
    {
        GameObject bullet = _bulletPool.Spawn(muzzlePos, rotation);
        if (bullet.TryGetComponent<PooledObject>(out var pooled))
        {
            pooled.DespawnAfter(3f);
        }
    }
}
```

For typed `Component` pooling in code without a `MonoBehaviour` wrapper, use `ComponentPool<T>`.

---

## Step 3: Hierarchical State Machine (HFSM)

### Building and Running an FSM

```csharp
using ContentContent.Core.Fsm;
using ContentContent.Core.Fsm.Unity;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private StateMachineRunner _runner;
    private bool _seesPlayer;
    private bool _isDead;

    private void Start()
    {
        var idle = new ActionState("Idle", onEnter: () => Debug.Log("Idle"));
        var chase = new ActionState("Chase", onTick: dt => MoveTowardPlayer(dt));
        var dead = new ActionState("Dead", onEnter: () => Debug.Log("Defeated"));

        var fsm = new StateMachine(idle);
        fsm.AddTransition(idle, chase, () => _seesPlayer);
        fsm.AddTransition(chase, idle, () => !_seesPlayer);

        // Global high-priority transition from any state
        fsm.AddAnyTransition(dead, () => _isDead, priority: 100);

        _runner.Initialize(fsm, enterImmediately: true);
    }

    private void MoveTowardPlayer(float dt) { /* ... */ }
}
```

### Nesting Sub-State Machines

Because `StateMachine` implements `IState`, pass a child `StateMachine` anywhere an `IState` is expected:

```csharp
var combatSubFsm = new StateMachine(aimState);
combatSubFsm.AddTransition(aimState, shootState, () => isTargetLocked);

var rootFsm = new StateMachine(patrolState);
rootFsm.AddTransition(patrolState, combatSubFsm, () => hasEnemy);
rootFsm.AddTransition(combatSubFsm, patrolState, () => !hasEnemy);
```

When `rootFsm` transitions out of `combatSubFsm`, `combatSubFsm.OnExit()` automatically exits its active child state as well.

---

## Step 4: 2D Grid

### Pure C# Grid Storage & Events

```csharp
using System.Collections.Generic;
using ContentContent.Core.Grid;

var grid = new Grid2D<int>(width: 16, height: 16, initialFactory: _ => 0);

grid.OnCellChanged += (coord, oldVal, newVal) =>
{
    // React to cell changes (e.g., update visuals or invalidate caches)
};

grid[4, 2] = 1;

var neighbors = new List<GridCoord>(8);
grid.GetNeighbors(new GridCoord(4, 2), neighbors, includeDiagonals: true);
```

### Mapping World Space in Unity (`GridBehaviour` & `UnityGridTransform`)

1. Add `GridBehaviour` to a GameObject in your scene.
2. Configure **Width**, **Height**, **Cell Size**, and **Plane** (`XY` for 2D or `XZ` for 3D top-down). Selecting the object draws a Scene-view wireframe Gizmo.
3. Create a typed grid from the component and convert world positions:

```csharp
using ContentContent.Core.Grid;
using ContentContent.Core.Grid.Unity;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    [SerializeField] private GridBehaviour _gridBehaviour;
    private Grid2D<bool> _occupied;

    private void Awake()
    {
        _occupied = _gridBehaviour.CreateGrid<bool>(_ => false);
    }

    public bool TryPlaceAtWorldPosition(Vector3 worldPos)
    {
        GridCoord coord = _gridBehaviour.WorldToGrid(worldPos);
        if (!_occupied.TryGet(coord, out bool isOccupied) || isOccupied)
            return false;

        _occupied[coord] = true;
        Vector3 snapCenter = _gridBehaviour.GridToWorldCenter(coord);
        // Place object at snapCenter...
        return true;
    }
}
```

For A* pathfinding over `IGrid<TCell>`, see the [Navigation Tutorial](../navigation/tutorial.md).

---

## Step 5: Run EditMode Tests

1. Open **Window → General → Test Runner**.
2. Switch to the **EditMode** tab.
3. Expand **ContentContent.Core.Tests** and click **Run All** to verify `ObjectPoolTests`, `StateMachineTests`, and `GridTests`.
