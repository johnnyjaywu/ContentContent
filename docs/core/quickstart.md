# Core Quickstart

`com.contentcontent.core` provides seven lightweight gameplay building blocks — **Blackboard**, **HFSM**, **EventBus**, **Timers & Cooldowns**, **2D/3D Grid**, **Deterministic RNG**, and **Commands** — ready to inspect in the Unity Editor or call directly from pure C#.

---

## 1. Install

Open **Window → Package Manager → + → Add package from git URL...** and enter:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.core
```

> **Try the Sample:** In **Package Manager → ContentContent - Core → Downloadable Samples**, download **Core Playground**, click **Open Scene**, and press **Play** to test every Core module interactively.

---

## 2. Scene Components (Zero Boilerplate)

Create any Core scene component from **GameObject → ContentContent → Core →**:
- **Blackboard** — Seed typed initial keys (`Bool`, `Int`, `Float`, `String`) in the Inspector, assign an optional parent scope, and watch live values update in Play Mode.
- **Event Bus** — Scopes a zero-boxing event bus to a scene or prefab and shows active channels & subscriber counts in Play Mode.
- **State Machine Runner** — Drives an HFSM in `Update`/`FixedUpdate` and exposes an `OnStateChanged(string)` `UnityEvent` for UI/audio hooks.
- **Timer Scheduler Runner** — Ticks timers with per-scheduler `TimeScale` and Play Mode inspection.
- **Grid** — Visualizes a 2D `XY` or 3D `XZ` grid in the Scene view and converts world positions to grid coordinates.

---

## 3. Common C# Recipes

### Blackboard + Hierarchical State Machine (HFSM)
```csharp
using ContentContent.Core.Blackboards;
using ContentContent.Core.Fsm;

var board = new Blackboard();
var alertKey = BlackboardKey.Create<bool>("IsAlert");

var patrol = new ActionState("Patrol");
var chase = new ActionState("Chase");
var fsm = new StateMachine(patrol);

fsm.AddTransitionWhen(patrol, chase, board, alertKey, alert => alert);
fsm.AddTransitionWhen(chase, patrol, board, alertKey, alert => !alert);
fsm.OnEnter();
```

### Zero-Boxing EventBus
```csharp
using ContentContent.Core.Events;

public readonly struct ScoreChanged { public readonly int Score; public ScoreChanged(int s) => Score = s; }

var bus = new EventBus();
using var sub = bus.Subscribe<ScoreChanged>((in ScoreChanged e) => { /* ... */ });
bus.Publish(new ScoreChanged(100));
```

### Timers, Cooldowns, Deterministic RNG & Undo Commands
```csharp
using ContentContent.Core.Commands;
using ContentContent.Core.Random;
using ContentContent.Core.Timers;

// Cooldown & Timers
var dashCd = new Cooldown(1.5f);
if (dashCd.TryConsume()) { /* dash */ }
dashCd.Tick(dt);

var timers = new TimerScheduler();
TimerHandle burn = timers.Every(1f, () => ApplyBurn(), repeatCount: 3);
timers.Tick(dt);

// Reproducible loot rolls
var rng = new Xoshiro128StarStar(seed: 42UL);
var table = new WeightedTable<string>();
table.Add("Coin", 80f);
table.Add("Gem", 20f);
string loot = table.Pick(rng);

// Undo / Redo transactions
var history = new CommandHistory(maxCapacity: 32);
history.Execute(new ActionCommand(() => gold += 10, () => gold -= 10));
history.Undo();
```
