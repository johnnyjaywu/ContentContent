# Core Package Quickstart

This guide walks through installing `com.contentcontent.core` and using its foundational sub-domains: **HFSM**, **2D/3D Grid**, **EventBus**, **Timers & Cooldowns**, **Deterministic RNG**, **Commands**, and **Blackboard**.

---

## Step 1: Install the Package

Open **Window → Package Manager → + → Add package from git URL...** and enter:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.core
```

If you are working inside the `ContentContent` repository directly, the package is already present at `Packages/com.contentcontent.core`.

### Try the Built-In Sample

1. Open **Window → Package Manager**, select **ContentContent - Core**, scroll to **Downloadable Samples**, and click **Download** on **Core Playground** (installs into `Assets/Samples/ContentContent/Core/CorePlayground/`).
2. Click **Open Scene** (or open `CorePlayground.unity`) and press **Play** to test HFSM + `Blackboard`, `EventBus`, `TimerScheduler`, `Cooldown`, `Xoshiro128StarStar` + `WeightedTable`, `CommandHistory`, and `Grid2D` interactively via the on-screen HUD.

---

## Step 2: Hierarchical State Machine (HFSM)

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

## Step 3: 2D Grid

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

For A* pathfinding over grids, see the [Navigation Quickstart](../navigation/quickstart.md).

---

## Step 4: EventBus

```csharp
using ContentContent.Core.Events;

public readonly struct EnemyDefeatedEvent
{
    public readonly int Xp;
    public EnemyDefeatedEvent(int xp) => Xp = xp;
}

var bus = new EventBus();
using var sub = bus.Subscribe<EnemyDefeatedEvent>((in EnemyDefeatedEvent e) =>
{
    // Zero-boxing struct event dispatch
}, priority: 10);

bus.Publish(new EnemyDefeatedEvent(50));
```

---

## Step 5: Timers & Cooldowns

```csharp
using ContentContent.Core.Timers;

var scheduler = new TimerScheduler();
TimerHandle poison = scheduler.Every(1.0f, () => ApplyPoisonTick(), repeatCount: 5);
scheduler.Tick(deltaTime);

// Standalone allocation-free ability cooldown
var dashCooldown = new Cooldown(duration: 2.5f);
if (dashCooldown.TryConsume())
{
    PerformDash();
}
dashCooldown.Tick(deltaTime);
```

---

## Step 6: Deterministic RNG & Weighted Tables

```csharp
using ContentContent.Core.Random;

var rng = new Xoshiro128StarStar(seed: 1337UL);
RngState checkpoint = rng.GetState();

var loot = new WeightedTable<string>();
loot.Add("Gold", 70f);
loot.Add("Potion", 25f);
loot.Add("Relic", 5f);

string drop = loot.Pick(rng);
rng.SetState(checkpoint); // Exact deterministic replay
```

---

## Step 7: Commands & Undo/Redo

```csharp
using ContentContent.Core.Commands;

var history = new CommandHistory(maxCapacity: 50);
int gold = 100;

using (history.BeginTransaction())
{
    history.Execute(new ActionCommand(() => gold -= 25, () => gold += 25));
    history.Execute(new ActionCommand(() => gold -= 10, () => gold += 10));
}

history.Undo(); // Reverts both commands in one atomic step (gold == 100)
history.Redo(); // Re-applies both (gold == 65)
```

---

## Step 8: Blackboard & Opt-In HFSM Composition

```csharp
using ContentContent.Core.Blackboards;
using ContentContent.Core.Fsm;

var squadBoard = new Blackboard();
var agentBoard = new Blackboard(parent: squadBoard);

var alertKey = BlackboardKey.Create<bool>("SquadAlerted");
squadBoard.Set(alertKey, true);

var fsm = new StateMachine(patrolState);
fsm.AddTransitionWhen(patrolState, combatState, agentBoard, alertKey, alerted => alerted);
```

---

## Step 9: Run EditMode Tests

1. Open **Window → General → Test Runner** (or run `dotnet test tools/PureTests/ContentContent.PureTests.csproj --no-restore`).
2. Switch to the **EditMode** tab.
3. Expand **ContentContent.Core.Tests** and click **Run All**.
