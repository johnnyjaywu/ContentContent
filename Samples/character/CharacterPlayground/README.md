# Character Playground Sample

Drop-in interactive character controller demo showcasing `com.contentcontent.character`:

- **Unified 2D & 3D `CharacterMotor`** — Switch live between `SideScroller2D`, `TopDown2D`, and `ThirdPerson3D` modes.
- **Core HFSM `CharacterController`** — Live `Idle`, `Move`, `Airborne`, and `Dash` state transitions synchronized with `Blackboard`.
- **Jump & Dash Feel** — Coyote time, jump buffering, variable jump height, double jump, air dash, and knockback impulses.
- **Extensible Actions (`CharacterActionRunner`)** — Custom cooldown-gated `"Attack"` action using `ActionLockFlags.BlockMovement | ActionLockFlags.BlockJump`.

## Setup

1. Create an empty `GameObject` in any scene.
2. Attach `CharacterPlaygroundSample`.
3. Press **Play** and control with **WASD / Arrow Keys** (Move), **Space** (Jump), **Left Shift** (Dash), **F** (Attack), or the on-screen buttons.
