# ContentContent

A collection of modular, reusable Unity game systems structured as independent **Unity Package Manager (UPM)** packages.

**Documentation:** https://johnnyjaywu.github.io/ContentContent/ (source in [`docs/`](docs/))

## Architecture

Each system lives in its own UPM package under `Packages/` and follows a strict separation of concerns:

- **`Runtime/`** (`noEngineReferences: true`): Pure C# logic with zero `UnityEngine` dependencies — deterministic, fast to compile, and easy to unit test.
- **`Runtime/Unity/`**: Thin `MonoBehaviour` and `ScriptableObject` adapters that bridge the core logic to Unity's lifecycle, Inspector, and Gizmos.
- **`Tests/EditMode/`**: NUnit EditMode tests (automatically enabled when `com.unity.test-framework` is installed, and runnable directly via `dotnet test`).
- **`Samples/` (On-Demand Download):** All samples live in top-level [`Samples/`](Samples/) (not bundled inside UPM packages) and are downloaded on demand inside Unity Package Manager via the **Downloadable Samples** extension shipped with `com.contentcontent.core`.

## Packages

| Package | Path | Description | Samples | Docs |
| --- | --- | --- | --- | --- |
| **Core** *(Foundational Systems)* | `Packages/com.contentcontent.core` | Foundational zero-dependency C# utilities with Unity adapters: `ObjectPool<T>` & `PrefabPoolBehaviour`; hierarchical `StateMachine` & `StateMachineRunner`; `Grid2D<TCell>` / `Grid3D<TCell>` & `GridBehaviour`; zero-boxing `EventBus` & `EventBusBehaviour`; `TimerScheduler`, `Cooldown`, & `TimerSchedulerRunner`; zero-reflection scoped `ServiceContainer` & `SceneServiceScope`; deterministic `Xoshiro128StarStar` RNG & `WeightedTable<T>`; `CommandHistory` (undo/redo, merging, transactions) & `CommandQueue`; and zero-boxing scoped `Blackboard`, `BlackboardBehaviour`, & HFSM extensions. | Core Playground | [Core Docs](docs/core/how-it-works.md) |
| **Navigation** | `Packages/com.contentcontent.navigation` | Zero-boilerplate 2D (`XY`/`XZ`) & volumetric 3D navigation: compact 1-byte `NavGrid`, zero-GC flat-array `NavPathfinder` with LOS string-pulling, `NavPathQueue`, `GridPathfinder`, and drop-in `NavSurface`, `NavAgent`, & `NavObstacle` Unity components. | Navigation Playground | [Navigation Docs](docs/navigation/how-it-works.md) |
| **Stats, Buffs & Health** | `Packages/com.contentcontent.stats` | Dirty-cached `Stat` modifiers (`Flat`, `PercentAdd`, `PercentMult`), `StatCollection<T>`, timed/stacking `BuffController<T>`, `Health`/`IDamageable`, `StatSheetAsset`, `StatsComponent`, and `HealthComponent`. | Combat Stats | [Stats Docs](docs/stats/how-it-works.md) |
| **Dialog** | `Packages/com.contentcontent.dialog` | Branching `DialogGraph` & `DialogRunner` with typed `DialogVariableStore`, conditions, revisit-safe choice mutations, `{variable}` interpolation, commands, `IDialogLocalizer`, `IDialogView`, and ScriptableObject adapters. | uGUI & UI Toolkit | [Dialog Docs](docs/dialog/how-it-works.md) |
| **Character** | `Packages/com.contentcontent.character` | Unified 2D (`SideScroller2D` / `TopDown2D`) & 3D (`ThirdPerson3D`) kinematic `CharacterMotor` (wall slide/jump/climb, coyote time, jump buffer, variable jump height, multi-jump, dash, pogo bounce, moving/one-way platforms), Core HFSM `CharacterController`, `InputBuffer`, cooldown-gated `CharacterActionRunner` with lock flags, and `CharacterMotor2D`/`CharacterMotor3D` Unity adapters. | Character Playground | [Character Docs](docs/character/how-it-works.md) |
| **Camera** | `Packages/com.contentcontent.camera` | Frame-rate-independent pure-C# `CameraDirector` (turn-delayed look-ahead, grounded vertical peek, priority `CameraZoneStack`, zoom smoothing, snap cuts, impulse cues), `CameraDirectorBehaviour`, `CameraZoneTrigger2D`, and Cinemachine 3 `CinemachineCameraBridge`. | Camera Playground | [Camera Docs](docs/camera/how-it-works.md) |
| **World** | `Packages/com.contentcontent.world` | Metroidvania `WorldGraph` with ability/flag gates, static `WorldReachabilityValidator` (catches soft-locks & trapped drops), `CheckpointService` (bench checkpoints vs. safe-ground hazard respawn), `RoomLifecycleController` & `IResettable`, `WorldFlagStore`, and 2D Unity adapters. | World Playground | [World Docs](docs/world/how-it-works.md) |

## Installing in Another Unity Project

Open **Window → Package Manager → + → Add package from git URL...** and enter the URL for any package you want:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.navigation
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.stats
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.dialog
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.character
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.camera
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.world
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

### Character Controller (2D & 3D)
```csharp
using System.Numerics;
using ContentContent.Character;

var config = new CharacterConfig { Mode = MovementMode.SideScroller2D, MaxAirJumps = 1 };
var controller = new CharacterController(config);

var input = new CharacterInput { Move = new Vector3(1f, 0f, 0f), JumpPressed = true, JumpHeld = true };
Vector3 velocity = controller.Tick(deltaTime, in input, MotorSensors.Grounded);
```

## Downloading Samples Inside Unity

Samples are hosted as separate `.zip` archives on GitHub Releases (`samples-latest`) rather than bundled inside UPM packages:

1. Install **`com.contentcontent.core`** (plus any other **ContentContent** package you want to try).
2. Open **Window → Package Manager**, select any **`ContentContent - ...`** package, and scroll to the **Downloadable Samples** section in the details panel.
3. Click **Download** (or **Update** / **Open Scene** / **Ping** / **Remove**) — each sample installs cleanly into `Assets/Samples/ContentContent/` with SHA-256 verification and ships a ready-to-play `.unity` scene. *(Inside a clone of this repo, `Samples/` is also used as a local fallback automatically.)*

## Running Tests

### Fast Pure C# Tests & Hygiene Check (~2s, no Unity required)

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify-repo.ps1
```

Or run only the NUnit pure-C# test suite directly:

```powershell
dotnet test tools/PureTests/ContentContent.PureTests.csproj
```

### Unity EditMode Tests

1. Install **Test Framework** (`com.unity.test-framework`) via Unity Package Manager if not already installed.
2. Open **Window → General → Test Runner**, select the **EditMode** tab, and click **Run All** (or run `scripts/run-unity-tests.ps1` in batchmode with Unity closed).
