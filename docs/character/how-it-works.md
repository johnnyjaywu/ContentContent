# How Character Works

`com.contentcontent.character` provides a unified **2D (`SideScroller2D` / `TopDown2D`) and 3D (`ThirdPerson3D`)** character locomotion motor, Core HFSM state controller, input buffer, and extensible cooldown-gated action system — split cleanly between a pure C# core (`noEngineReferences: true`) and thin Unity adapters.

## Package Architecture

```text
Packages/com.contentcontent.character/
├── package.json                                # Depends on com.contentcontent.core
├── Runtime/
│   ├── ContentContent.Character.asmdef         # Pure C# (noEngineReferences: true)
│   ├── MovementMode.cs                         # MovementMode & ActionLockFlags enums
│   ├── CharacterConfig.cs                      # Serializable locomotion, jump, dash, wall & platform config + presets
│   ├── CharacterInput.cs                       # CharacterInput & MotorSensors structs
│   ├── InputBuffer.cs                          # Deterministic named input buffer
│   ├── CharacterEvents.cs                      # Zero-copy struct events for Core EventBus
│   ├── CharacterBlackboardKeys.cs              # Typed Core Blackboard keys
│   ├── CharacterMotor.cs                       # Sensor-in / velocity-out kinematic motor
│   ├── CharacterContext.cs                     # Shared context for HFSM states & actions
│   ├── CharacterAction.cs                      # ICharacterAction, CharacterActionBase & CharacterActionRunner
│   ├── CharacterStates.cs                      # Built-in Idle, Move, Airborne, WallSlide, WallClimb & Dash HFSM states
│   ├── CharacterController.cs                  # High-level composition (Motor + Actions + HFSM + Blackboard)
│   └── Unity/
│       ├── ContentContent.Character.Unity.asmdef
│       ├── CharacterVectorExtensions.cs        # Unity Vector2/Vector3 <-> System.Numerics.Vector3
│       ├── CharacterConfigAsset.cs             # ScriptableObject config preset
│       ├── ISurfaceVelocityProvider.cs         # Moving platform / conveyor velocity interface
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
- **`CharacterInput`**: desired `Move` direction, `JumpPressed`, `JumpHeld`, `JumpReleased`, `DashPressed`, `GrabHeld`, `DropThroughPressed`
- **`MotorSensors`**: `IsGrounded`, `GroundNormal`, `IsTouchingCeiling`, `IsTouchingWall`, `WallNormal`, `PlatformVelocity`, `IsOnOneWayPlatform`
- **`ActionLockFlags`**: combined manual locks + active action locks

And produces the updated **`Velocity`** and **`FacingDirection`**, firing C# events (`OnJumped`, `OnWallJumped`, `OnLanded`, `OnDashed`) and optional Core `IEventBus` struct events.

Supported spatial modes (`MovementMode`):
- **`SideScroller2D`**: horizontal movement on `X`, gravity, jumping, wall slide/jump/climb on `Y`, `Z` forced to `0`.
- **`TopDown2D`**: planar movement on `X` and `Y`, no gravity (`IsGrounded` always `true`), `Z` forced to `0`.
- **`ThirdPerson3D`**: ground movement on `XZ`, gravity and jumping on `Y`.

### 2. Precision Platformer Feel, Wall Traversal & Dash Mechanics

Built directly into `CharacterMotor` (with `CharacterConfig.CreateHollowKnightPreset()` and `CharacterConfig.CreateCelestePreset()` presets):
- **Acceleration & Deceleration**: separate `GroundAcceleration`/`GroundDeceleration` and `AirAcceleration`/`AirDeceleration`.
- **Coyote Time & Jump Buffering**: `CoyoteTime` preserves grounded jump eligibility after stepping off a ledge; `JumpBufferTime` buffers early presses and automatically preserves short hops if released before touchdown.
- **Variable Jump Height, Apex Hang & Fast-Fall**: releasing jump scales upward speed by `JumpCutMultiplier`; `ApexVelocityThreshold` + `ApexGravityMultiplier` soften peak hang time; `FastFallMaxSpeed` supports down-held dives.
- **Multi-Jump & Pogo Bounce**: `MaxAirJumps` tracks mid-air jumps; `ApplyBounce()` overrides vertical velocity upward and optionally refills air jumps and air dashes (e.g., Hollow Knight down-slash pogo).
- **Wall Slide, Wall Jump & Optional Wall Climb**: `IsTouchingWall`/`WallNormal` clamp wall-slide fall speed (`WallSlideMaxFallSpeed`), allow wall jumps with `WallJumpCoyoteTime` and `WallJumpLockTime` steering lock (plus configurable air jump/dash refills), and optionally enable stamina-gated wall grab/climb (`EnableWallClimb`, `ClimbStamina`).
- **Dash (Horizontal or 8-Way)**: directional burst (`DashSpeed`, `DashDuration`, `DashEndSpeedMultiplier`), gated by Core's `Cooldown` (`DashCooldown`), buffered via `DashBufferTime`, with optional `AllowOmnidirectionalDash2D`, `DashSuspendsGravity`, and `AllowAirDash`/`MaxAirDashes`.
- **Moving & One-Way Platforms**: carries `PlatformVelocity` cleanly while grounded, inherits jump launch momentum without double-adding, and supports timed one-way drop-through (`DropThroughPressed`).
- **External Impulses**: `ApplyImpulse(Vector3)` adds knockback or launch pad velocity that decays smoothly at `KnockbackDecay`.

### 3. Core HFSM + Extensible Action & Lock Layer

`CharacterController` composes:
- **Core `StateMachine`**: pre-wired with `Idle`, `Move`, `Airborne`, `WallSlide`, `WallClimb`, and `Dash` states (`StateBase<CharacterContext>`). You can add custom states and transitions directly on `controller.StateMachine`.
- **`CharacterActionRunner`**: runs registered `ICharacterAction` / `CharacterActionBase` instances (attacks, dodges, hitstun, casts) with Core `Cooldown` gating, priority-based interruption (`CanBeInterruptedBy`), buffered action triggers, and `ActionLockFlags` (`BlockMovement`, `BlockTurn`, `BlockJump`, `BlockDash`, `SuspendGravity`, `All`).
- **Core `Blackboard`**: automatically updated every tick via `CharacterBlackboardKeys` (`Velocity`, `Speed`, `PlanarSpeed`, `VerticalSpeed`, `FacingDirection`, `IsGrounded`, `IsDashing`, `IsWallSliding`, `IsWallClimbing`, `WallNormal`, `AirJumpsRemaining`, `AirDashesRemaining`, `ClimbStamina`, `ActiveActionId`, `IsActionActive`).
- **Optional Core `IEventBus`**: publishes zero-copy struct events (`CharacterJumpedEvent`, `CharacterWallJumpedEvent`, `CharacterLandedEvent`, `CharacterDashedEvent`, `CharacterActionStartedEvent`, `CharacterActionEndedEvent`, `CharacterStateChangedEvent`).

### 4. Thin Unity Physics Adapters

- **`CharacterMotor2D`**: wraps `Rigidbody2D` (`gravityScale = 0`), performs non-allocating box overlap/cast checks for ground normals, ceiling corner correction nudges, left/right wall sensors, one-way platform drop-through (`PlatformEffector2D` / layer mask + `Physics2D.IgnoreCollision`), and moving platform velocity (`ISurfaceVelocityProvider` / `Rigidbody2D`), with Scene Gizmos and `FixedUpdate()` simulation.
- **`CharacterMotor3D`**: wraps Unity `CharacterController`, performs sphere ground/ceiling checks with Gizmos, and moves via `CharacterController.Move(velocity * dt)` in `Update()`.
- Both adapters are **input-agnostic** (`SetMoveInput`, `PressJump`, `ReleaseJump`, `PressDash`, `SetGrabHeld`, `PressDropThrough`, `TriggerAction`, `ApplyImpulse`, `ApplyBounce`) so you can wire Unity's New Input System, legacy Input, or AI scripts without coupling.
