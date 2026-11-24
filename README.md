# ContentContent

An open-source, modular collection of foundational game systems structured as independent **Unity Package Manager (UPM)** packages — designed so common workflows work code-free in the Editor while full C# control and transferable architecture remain clean underneath.

**Documentation:** https://johnnyjaywu.github.io/ContentContent/ (source in [`docs/`](docs/)) · **[Philosophy & Architecture](docs/architecture.md)**

## Architecture (4 Layers)

Each system lives in its own UPM package under `Packages/` and follows a strict 4-layer separation:

1. **`Runtime/`** (`noEngineReferences: true`): Pure C# simulation rules with zero `UnityEngine` dependencies — deterministic, fast to compile, and portable across engines.
2. **`Runtime/Unity/`**: Code-free `MonoBehaviour` and `ScriptableObject` authoring components with smart defaults, sibling auto-wiring, and `UnityEvent` hooks (plus optional isolated adapters such as `Runtime/Unity/Input/` via `versionDefines`).
3. **`Editor/`**: Custom Inspectors, mode-aware foldouts, inline setup validation with 1-click **Fix** buttons, creation menus, and live PlayMode telemetry.
4. **`Tests/EditMode/` & `Samples/`**: NUnit EditMode / `dotnet test` suites and downloadable playable sample scenes built under top-level [`Samples/`](Samples/) via the **Downloadable Samples** extension in `com.contentcontent.core`.

## Packages

| Package | Path | Description | Samples | Docs |
| --- | --- | --- | --- | --- |
| **Core** *(Foundational Systems)* | `Packages/com.contentcontent.core` | Foundational zero-dependency C# utilities with Unity adapters: hierarchical `StateMachine` & `StateMachineRunner`; `Grid2D<TCell>` / `Grid3D<TCell>` & `GridBehaviour`; zero-boxing `EventBus` & `EventBusBehaviour`; `TimerScheduler`, `Cooldown`, & `TimerSchedulerRunner`; deterministic `Xoshiro128StarStar` RNG & `WeightedTable<T>`; `CommandHistory` (undo/redo, merging, transactions) & `CommandQueue`; and zero-boxing scoped `Blackboard`, `BlackboardBehaviour`, & HFSM extensions. | Core Playground | [Quickstart](docs/core/quickstart.md) · [Design](docs/core/design.md) |
| **Character** | `Packages/com.contentcontent.character` | Unified 2D (`SideScroller2D` / `TopDown2D`) & 3D (`ThirdPerson3D`) kinematic `CharacterMotor` (wall slide/jump/climb, coyote time, jump buffer, variable jump height, multi-jump, dash, pogo bounce, moving/one-way platforms), Core HFSM `CharacterController`, `CharacterActionAsset`, code-free `CharacterInput2D`/`3D`, `CharacterAnimatorBridge`, `MovingPlatform2D`, and custom Editor Inspectors. | Character Playground | [Quickstart](docs/character/quickstart.md) · [Design](docs/character/design.md) |
| **Stats, Buffs & Health** | `Packages/com.contentcontent.stats` | Dirty-cached `Stat` modifiers (`Flat`, `PercentAdd`, `PercentMult`), `StatCollection<T>`, timed/stacking `BuffController<T>`, `Health`/`IDamageable`, `StatSheetAsset`, `BuffAsset`, auto-binding `StatsComponent` / `HealthComponent`, and live telemetry Inspectors. | Combat Stats | [Quickstart](docs/stats/quickstart.md) · [Design](docs/stats/design.md) |
| **Dialog** | `Packages/com.contentcontent.dialog` | Branching `DialogGraph` & `DialogRunner` with typed `DialogVariableStore`, conditions, `{variable}` interpolation, commands, `IDialogLocalizer`, `IDialogView`, Yarn-style `.txt` / `DialogGraphAsset` authoring, code-free `CommandBindings`, `ValueData` drawer, and live PlayMode debugger. | uGUI & UI Toolkit | [Quickstart](docs/dialog/quickstart.md) · [Design](docs/dialog/design.md) |
| **Navigation** | `Packages/com.contentcontent.navigation` | Zero-boilerplate 2D (`XY`/`XZ`) & volumetric 3D navigation: compact 1-byte `NavGrid`, zero-GC flat-array `NavPathfinder` with LOS string-pulling, `NavPathQueue`, drop-in `NavSurface`, `NavAgent`, `NavObstacle`, `NavPatrol`, `NavFollowTarget`, and Editor diagnostics. | Navigation Playground | [Quickstart](docs/navigation/quickstart.md) · [Design](docs/navigation/design.md) |
| **World** | `Packages/com.contentcontent.world` | Genre-agnostic `WorldGraph` & `WorldGraphAsset` with ability/flag gates, static `WorldReachabilityValidator` (catches soft-locks & unreachable rooms), `CheckpointService` (hard checkpoints vs. safe-ground hazard respawn), `RoomLifecycleController` & `ResettableTransform`, `WorldFlagStore`, and 2D Unity triggers. | World Playground | [Quickstart](docs/world/quickstart.md) · [Design](docs/world/design.md) |
| **Save** | `Packages/com.contentcontent.save` | Versioned `SaveManager`, `ISaveable` participants, sequential `ISaveMigration` upgraders, atomic `.tmp`/`.bak` `FileSaveStorage` & `InMemorySaveStorage`, invariant `SaveJsonCodec`, and `SaveManagerBehaviour` / `SaveableTransform`. | Save Playground | [Quickstart](docs/save/quickstart.md) · [Design](docs/save/design.md) |
| **Interaction** | `Packages/com.contentcontent.interaction` | Context-sensitive `InteractionSelector` (distance/facing/priority scoring), tap/hold prompts, condition locks, non-alloc 2D/3D `InteractorBehaviour` & `InteractableBehaviour`, and isolated `InteractionInput` adapter. | Interaction Playground | [Quickstart](docs/interaction/quickstart.md) · [Design](docs/interaction/design.md) |

## Installing in Another Unity Project

Open **Window → Package Manager → + → Add package from git URL...** and enter the URL for any package you want:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.character
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.navigation
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.stats
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.dialog
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.world
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.save
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.interaction
```

## Quick Examples

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

### 2D & Volumetric 3D Navigation
```csharp
using System.Collections.Generic;
using ContentContent.Core.Grid;
using ContentContent.Navigation;

var navGrid = new NavGrid(16, 16, 16, NavMode.Volumetric3D);
navGrid.SetWalkable(5, 5, 5, false); // obstacle voxel

var pathfinder = new NavPathfinder();
var path = new List<GridCoord3D>();
bool found = pathfinder.TryFindPath(
    navGrid,
    new GridCoord3D(0, 0, 0),
    new GridCoord3D(15, 12, 15),
    path);
```

### Character Controller (2D & 3D)
```csharp
using System.Numerics;
using ContentContent.Character;

var config = CharacterConfig.CreateActionPlatformerPreset();
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
