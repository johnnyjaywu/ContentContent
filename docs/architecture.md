# Philosophy & Architecture

ContentContent is built around three practical ideas so you can **make games quickly without fighting code**, **learn real engineering habits as you go**, and **keep complete control over every line of source code**.

---

## 1. Build What Engines Leave Out

Game engines give you rendering, physics colliders, audio playback, and UI buttons — the raw hardware of a game. They usually leave out the *gameplay systems* every project ends up rebuilding:

- How a jump feels forgiving and crisp instead of floaty (**Character**).
- How swords, potions, and timed debuffs modify stats cleanly without bugs (**Stats**).
- How branching conversations track choices and trigger game events without locking into one UI style (**Dialog**).
- How enemies navigate 2D side-view levels or full 3D flight volumes without static baking (**Navigation**).
- How worlds track unlocked areas, checkpoints, and soft-locks (**World**).
- How timers, state machines, undoable commands, blackboards, events, and deterministic RNG fit together cleanly (**Core**).

ContentContent intentionally avoids replacing built-in engine tools like `UnityEngine.Pool`, general dependency injection frameworks, or Cinemachine.

---

## 2. How Every Package Is Organized (The 4 Layers)

Every ContentContent package separates **what the game rules do** from **how Unity wires them to the scene**. Once you understand these four layers, you can understand every package in minutes — and carry the exact same pattern to Godot or Unreal.

```text
┌─────────────────────────────────────────────────────────────┐
│  4. Samples & Tests    — Playable scenes + automated proof  │
├─────────────────────────────────────────────────────────────┤
│  3. Editor Experience  — Smart Inspectors, 1-click Fixes,   │
│                          live PlayMode telemetry            │
├─────────────────────────────────────────────────────────────┤
│  2. Unity Authoring    — Drag-and-drop components, presets, │
│                          auto-wiring, UnityEvents           │
├─────────────────────────────────────────────────────────────┤
│  1. Pure C# Core       — Fast, deterministic rules with     │
│                          zero UnityEngine dependencies      │
└─────────────────────────────────────────────────────────────┘
```

### Layer 1: Pure Simulation Core (`Runtime/`)
All the real rules live in plain C# (`noEngineReferences: true`).
- **Why?** Because game math shouldn't need a scene, a `GameObject`, or a slow Editor domain reload just to verify that a double-jump or damage formula works.
- **Transferable:** You can copy Layer 1 code directly into a Godot C# project or port the exact structure to Unreal C++.

### Layer 2: Code-Free Unity Authoring (`Runtime/Unity/`)
Thin `MonoBehaviour` and `ScriptableObject` wrappers bridge Layer 1 into Unity's Inspector, physics, Input System, and `UnityEvent`s.
- **Auto-wiring:** Components configure safe defaults when added (`Reset()`) and automatically locate sibling components so you don't have to write tedious glue scripts.
- **Full escape hatch:** Every wrapper exposes its underlying pure-C# engine via properties if you prefer controlling everything from code.

### Layer 3: Fool-Proof Editor Tooling (`Editor/`)
Custom Inspectors hide irrelevant settings based on the active mode, explain what each knob does in plain English, flag misconfigured colliders or layer masks with **1-click Fix** buttons, and show live state readouts in Play Mode.

### Layer 4: Tests & Playable Samples (`Tests/EditMode/` & `Samples/`)
Every system proves its rules with fast automated unit tests and ships a real, inspectable sample scene built using the exact same components you drop into your own game.

---

## 3. Code-Free First, Open-Source Always

- **Start by playing around:** Create a pre-configured object from **GameObject → ContentContent → ...**, press **Play**, and tune presets live in the Inspector.
- **Grow into C# naturally:** When a custom mechanic goes beyond Inspector settings, every class is readable, documented, compiled from source (never hidden inside DLLs), and structured as clean examples of industry patterns like *Sensor-In / Velocity-Out*, *Hierarchical State Machines*, *Command History*, and *Observer / Event Bus*.
