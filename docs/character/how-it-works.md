# How Character Works

`com.contentcontent.character` provides a unified **2D (`SideScroller2D` / `TopDown2D`) and 3D (`ThirdPerson3D`)** character locomotion motor, Core HFSM state controller, input buffer, and extensible cooldown-gated action system — split cleanly between a pure C# core (`noEngineReferences: true`) and thin Unity adapters.

## Package Architecture

```text
Packages/com.contentcontent.character/
├── package.json                                # Depends on com.contentcontent.core
├── Runtime/
│   ├── ContentContent.Character.asmdef         # Pure C# (noEngineReferences: true)
│   ├── MovementMode.cs                         # MovementMode & ActionLockFlags enums
│   ├── CharacterConfig.cs                      # Serializable locomotion, jump, dash & knockback config
│   ├── CharacterInput.cs                       # CharacterInput & MotorSensors structs
│   ├── InputBuffer.cs                          # Deterministic named input buffer
│   ├── CharacterEvents.cs                      # Zero-copy struct events for Core EventBus
│   ├── CharacterBlackboardKeys.cs              # Typed Core Blackboard keys
│   ├── CharacterMotor.cs                       # Sensor-in / velocity-out kinematic motor
│   ├── CharacterContext.cs                     # Shared context for HFSM states & actions
│   ├── CharacterAction.cs                      # ICharacterAction, CharacterActionBase & CharacterActionRunner
│   ├── CharacterStates.cs                      # Built-in Idle, Move, Airborne & Dash HFSM states
│   ├── CharacterController.cs                  # High-level composition (Motor + Actions + HFSM + Blackboard)
│   └── Unity/
│       ├── ContentContent.Character.Unity.asmdef
│       ├── CharacterVectorExtensions.cs        # Unity Vector2/Vector3 <-> System.Numerics.Vector3
│       ├── CharacterConfigAsset.cs             # ScriptableObject config preset
│       ├── CharacterMotor2D.cs                 # Rigidbody2D adapter (SideScroller2D & TopDown2D)
│       └── CharacterMotor3D.cs                 # Unity CharacterController adapter (ThirdPerson3D)
└── Tests/EditMode/
    ├── ContentContent.Character.Tests.asmdef
    ├── CharacterMotorTests.cs
    └── CharacterControllerAndActionTests.cs
```

---

## Core Concepts

### 1. Sensor-In / Velocity-Out Pure C# Motor (`CharacterMotor`)

`CharacterMotor` uses `System.Numerics.Vector3` and has zero `UnityEngine` dependencies. Every tick it consumes:
- **`CharacterInput`**: desired `Move` direction, `JumpPressed`, `JumpHeld`, `JumpReleased`, `DashPressed`
- **`MotorSensors`**: `IsGrounded`, `GroundNormal`, `IsTouchingCeiling`, `IsTouchingWall`, `WallNormal`
- **`ActionLockFlags`**: combined manual locks + active action locks

And produces the updated **`Velocity`** and **`FacingDirection`**, firing C# events (`OnJumped`, `OnLanded`, `OnDashed`) and optional Core `IEventBus` struct events.

Supported spatial modes (`MovementMode`):
- **`SideScroller2D`**: horizontal movement on `X`, gravity and jumping on `Y`, `Z` forced to `0`.
- **`TopDown2D`**: planar movement on `X` and `Y`, no gravity (`IsGrounded` always `true`), `Z` forced to `0`.
- **`ThirdPerson3D`**: ground movement on `XZ`, gravity and jumping on `Y`.

### 2. Platformer Jump Feel & Dash Mechanics

Built directly into `CharacterMotor`:
- **Acceleration & Deceleration**: separate `GroundAcceleration`/`GroundDeceleration` and `AirAcceleration`/`AirDeceleration`.
- **Coyote Time**: `CoyoteTime` preserves grounded jump eligibility briefly after stepping off a ledge.
- **Jump Buffering**: `JumpBufferTime` buffers early jump presses via `InputBuffer` and fires automatically on touchdown.
- **Variable Jump Height**: releasing jump while ascending scales vertical speed by `JumpCutMultiplier`.
- **Fall Gravity Multiplier & Max Fall Speed**: steeper falling arcs (`FallGravityMultiplier`) clamped to `MaxFallSpeed`.
- **Multi-Jump**: `MaxAirJumps` tracks mid-air jumps reset on landing.
- **Dash**: directional burst (`DashSpeed`, `DashDuration`), gated by Core's `Cooldown` (`DashCooldown`), buffered via `DashBufferTime`, with optional `DashSuspendsGravity` and `AllowAirDash`/`MaxAirDashes`.
- **External Impulses**: `ApplyImpulse(Vector3)` adds knockback or launch pad velocity that decays smoothly at `KnockbackDecay`.

### 3. Core HFSM + Extensible Action & Lock Layer

`CharacterController` composes:
- **Core `StateMachine`**: pre-wired with `Idle`, `Move`, `Airborne`, and `Dash` states (`StateBase<CharacterContext>`). You can add custom states and transitions directly on `controller.StateMachine`.
- **`CharacterActionRunner`**: runs registered `ICharacterAction` / `CharacterActionBase` instances (attacks, dodges, hitstun, casts) with Core `Cooldown` gating, priority-based interruption (`CanBeInterruptedBy`), buffered action triggers, and `ActionLockFlags` (`BlockMovement`, `BlockTurn`, `BlockJump`, `BlockDash`, `SuspendGravity`, `All`).
- **Core `Blackboard`**: automatically updated every tick via `CharacterBlackboardKeys` (`Velocity`, `Speed`, `PlanarSpeed`, `VerticalSpeed`, `FacingDirection`, `IsGrounded`, `IsDashing`, `AirJumpsRemaining`, `ActiveActionId`, `IsActionActive`).
- **Optional Core `IEventBus`**: publishes zero-copy struct events (`CharacterJumpedEvent`, `CharacterLandedEvent`, `CharacterDashedEvent`, `CharacterActionStartedEvent`, `CharacterActionEndedEvent`, `CharacterStateChangedEvent`).

### 4. Thin Unity Physics Adapters

- **`CharacterMotor2D`**: wraps `Rigidbody2D` (`gravityScale = 0`), performs box overlap ground/ceiling checks with Gizmos, and applies velocity in `FixedUpdate()`.
- **`CharacterMotor3D`**: wraps Unity `CharacterController`, performs sphere ground/ceiling checks with Gizmos, and moves via `CharacterController.Move(velocity * dt)` in `Update()`.
- Both adapters are **input-agnostic** (`SetMoveInput`, `PressJump`, `ReleaseJump`, `PressDash`, `TriggerAction`, `ApplyImpulse`) so you can wire Unity's New Input System, legacy Input, or AI scripts without coupling.
