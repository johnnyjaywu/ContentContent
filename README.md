# Game Systems

A collection of modular, reusable Unity game systems structured as independent **Unity Package Manager (UPM)** packages.

**Documentation:** https://johnnyjaywu.github.io/GameSystems/ (source in [`docs/`](docs/))

## Architecture

Each system lives in its own UPM package under `Packages/` and follows a strict separation of concerns:

- **`Runtime/`** (`noEngineReferences: true`): Pure C# logic with zero `UnityEngine` dependencies — deterministic, fast to compile, and easy to unit test.
- **`Runtime/Unity/`**: Thin `MonoBehaviour` and `ScriptableObject` adapters that bridge the core logic to Unity's lifecycle, Inspector, and Gizmos.
- **`Tests/EditMode/`**: NUnit EditMode tests (automatically enabled when `com.unity.test-framework` is installed).

## Packages

| Package | Path | Description | Docs |
| --- | --- | --- | --- |
| **Core** *(Foundational Systems)* | `Packages/com.contentcontent.core` | Foundational zero-dependency C# utilities with Unity adapters: `ObjectPool<T>` & `PrefabPoolBehaviour`; hierarchical `StateMachine` & `StateMachineRunner`; `Grid2D<TCell>` / `Grid3D<TCell>` & `GridBehaviour`; zero-boxing `EventBus` & `EventBusBehaviour`; `TimerScheduler`, `Cooldown`, & `TimerSchedulerRunner`; zero-reflection scoped `ServiceContainer` & `SceneServiceScope`; deterministic `Xoshiro128StarStar` RNG & `WeightedTable<T>`; `CommandHistory` (undo/redo, merging, transactions) & `CommandQueue`; and zero-boxing scoped `Blackboard`, `BlackboardBehaviour`, & HFSM extensions. | [Core Docs](docs/core/how-it-works.md) |
| **Navigation** | `Packages/com.contentcontent.navigation` | Zero-boilerplate 2D (`XY`/`XZ`) & volumetric 3D navigation: compact 1-byte `NavGrid`, zero-GC flat-array `NavPathfinder` with LOS string-pulling, `NavPathQueue`, `GridPathfinder`, and drop-in `NavSurface`, `NavAgent`, & `NavObstacle` Unity components. | [Navigation Docs](docs/navigation/how-it-works.md) |
| **Stats, Buffs & Health** | `Packages/com.contentcontent.stats` | Dirty-cached `Stat` modifiers (`Flat`, `PercentAdd`, `PercentMult`), `StatCollection<T>`, timed/stacking `BuffController<T>`, `Health`/`IDamageable`, `StatSheetAsset`, `StatsComponent`, and `HealthComponent`. | [Stats Docs](docs/stats/how-it-works.md) |
| **Dialog** | `Packages/com.contentcontent.dialog` | Branching `DialogGraph` & `DialogRunner` with typed `DialogVariableStore`, conditions, revisit-safe choice mutations, `{variable}` interpolation, commands, `IDialogLocalizer`, `IDialogView`, ScriptableObject adapters, and uGUI + UI Toolkit samples. | [Dialog Docs](docs/dialog/how-it-works.md) |
| **Character** | `Packages/com.contentcontent.character` | Unified 2D (`SideScroller2D` / `TopDown2D`) & 3D (`ThirdPerson3D`) kinematic `CharacterMotor` (accel/decel, coyote time, jump buffer, variable jump height, multi-jump, dash, knockback), Core HFSM `CharacterController`, `InputBuffer`, cooldown-gated `CharacterActionRunner` with lock flags, and `CharacterMotor2D`/`CharacterMotor3D` Unity adapters. | [Character Docs](docs/character/how-it-works.md) |

## Installing in Another Unity Project

Open **Window → Package Manager → + → Add package from git URL...** and enter the URL for any package you want:

```text
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.navigation
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.stats
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.dialog
https://github.com/johnnyjaywu/GameSystems.git?path=Packages/com.contentcontent.character
```

## Quick Examples

### Object Pooling
```csharp
using ContentContent.Core.Pooling;

var pool = new ObjectPool<ProjectileData>(() => new ProjectileData(), maxSize: 128);
using (pool.Get(out var item))
{
    // Automatically released back to the pool at the end of the scope
}
```

### Hierarchical State Machine
```csharp
using ContentContent.Core.Fsm;

var idle = new ActionState("Idle");
var patrol = new ActionState("Patrol");
var fsm = new StateMachine(idle);
fsm.AddTransition(idle, patrol, () => hasTarget);

fsm.OnEnter();
fsm.OnTick(Time.deltaTime);
```

### Stats, Buffs & Health
```csharp
using ContentContent.Stats;

var stats = new StatCollection<string>();
var maxHp = stats.RegisterStat("MaxHealth", 100f);
var buffs = new BuffController<string>(stats);

using var health = new Health(maxHp.Value);
health.BindMaxHealthStat(maxHp, MaxHealthChangePolicy.KeepPercentage);
health.ApplyDamage(new DamageInfo(25f, source: attacker));
```

### Grid & A* Pathfinding
```csharp
using System.Collections.Generic;
using ContentContent.Core.Grid;
using ContentContent.Navigation;

var grid = new Grid2D<bool>(16, 16, _ => true);
grid[5, 5] = false; // obstacle

var path = new List<GridCoord>();
bool found = GridPathfinder.TryFindPath(
    grid,
    new GridCoord(0, 0),
    new GridCoord(10, 10),
    isWalkable: (_, walkable) => walkable,
    outPath: path,
    allowDiagonals: true);
```

## Running Tests

1. Install **Test Framework** (`com.unity.test-framework`) via Unity Package Manager if not already installed.
2. Open **Window → General → Test Runner**, select the **EditMode** tab, and click **Run All**.
