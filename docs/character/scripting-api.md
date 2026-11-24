# Character Scripting API

Complete open-source API reference for `com.contentcontent.character`:
- **Pure C# Simulation:** `ContentContent.Character`
- **Unity Authoring & Physics:** `ContentContent.Character.Unity`
- **Unity Input System Bridge:** `ContentContent.Character.Unity.Input`

---

## Namespace: `ContentContent.Character`

### `MovementMode` & `ActionLockFlags` & `ActionAirRule` (enums)

- **`MovementMode`**: `SideScroller2D`, `TopDown2D`, `ThirdPerson3D`
- **`ActionLockFlags`** (`[Flags]`): `None`, `BlockMovement`, `BlockTurn`, `BlockJump`, `BlockDash`, `SuspendGravity`, `All`
- **`ActionAirRule`**: `Any`, `GroundedOnly`, `AirborneOnly`
- **`CharacterPresetType`**: `SideScroller`, `ActionPlatformer`, `PrecisionClimber`, `TopDown`, `ThirdPerson`

---

### `CharacterConfig` (`class`)

Serializable pure C# movement and game-feel configuration.

| Method | Return | Description |
| --- | --- | --- |
| `Clone()` | `CharacterConfig` | Deep-copies this configuration. |
| `CopyFrom(CharacterConfig other)` | `void` | Copies all fields from `other` in place and calls `Sanitize()`. |
| `Sanitize()` | `void` | Clamps numeric fields into safe ranges. |
| `Validate(List<string> warnings)` | `void` | Clears `warnings` and appends human-readable configuration diagnostics. |
| `CreatePreset(CharacterPresetType)` | `static CharacterConfig` | Creates a tuned configuration for the requested preset. |
| `CreateSideScrollerPreset()` | `static CharacterConfig` | Balanced 2D platformer preset. |
| `CreateActionPlatformerPreset()` | `static CharacterConfig` | Air-dash, double-jump, wall-refresh 2D preset. |
| `CreatePrecisionClimberPreset()` | `static CharacterConfig` | 8-way air dash, fast-fall, stamina wall-climb preset. |
| `CreateTopDownPreset()` | `static CharacterConfig` | Planar XY top-down preset. |
| `CreateThirdPersonPreset()` | `static CharacterConfig` | 3D XZ ground movement + Y jump/dash preset. |

---

### `CharacterInput` & `MotorSensors` (structs)

- **`CharacterInput`**: `Move` (`Vector3`), `JumpPressed`, `JumpHeld`, `JumpReleased`, `DashPressed`, `GrabHeld`, `DropThroughPressed`, `CharacterInput.None`.
- **`MotorSensors`**: `IsGrounded`, `GroundNormal`, `IsTouchingCeiling`, `IsTouchingWall`, `WallNormal`, `PlatformVelocity`, `IsOnOneWayPlatform`, `MotorSensors.Grounded`, `MotorSensors.Airborne`.

---

### `CharacterMotor` (`sealed class`)

Sensor-in / velocity-out kinematic motor (`System.Numerics.Vector3`).

| Member | Type / Return | Description |
| --- | --- | --- |
| `Velocity`, `LocomotionVelocity`, `ExternalVelocity`, `CarriedPlatformVelocity` | `Vector3` | Total and component velocities. |
| `FacingDirection`, `WallNormal` | `Vector3` | Normalized facing and contacted wall normals. |
| `IsGrounded`, `IsDashing`, `IsWallSliding`, `IsWallClimbing`, `IsDroppingThroughPlatform` | `bool` | Active contact and traversal flags. |
| `PlanarSpeed`, `VerticalSpeed` | `float` | Speed along movement plane and vertical axis. |
| `CoyoteTimeRemaining`, `WallCoyoteTimeRemaining`, `WallJumpLockRemaining` | `float` | Active grace and steering-lock timers. |
| `DashTimeRemaining`, `DashCooldownRemaining`, `IsDashCooldownReady` | `float` / `bool` | Dash burst and cooldown state. |
| `HasBufferedJump`, `HasBufferedDash` | `bool` | Whether jump or dash is currently buffered. |
| `AirJumpsRemaining`, `AirDashesRemaining`, `ClimbStamina` | `int` / `float` | Remaining mid-air charges and climb stamina. |
| `ManualLocks`, `ActiveLocks` | `ActionLockFlags` | External manual locks and combined active tick locks. |
| `OnJumped`, `OnWallJumped`, `OnLanded`, `OnDashed` | `event Action<...>` | Motor lifecycle events. |
| `Tick(float deltaTime, in CharacterInput, in MotorSensors, ActionLockFlags)` | `Vector3` | Advances motor simulation by `deltaTime` and returns velocity. |
| `RefreshConfig()` | `void` | Syncs live `Config` edits without clearing velocity. |
| `ApplyImpulse(Vector3)` / `ApplyBounce(float, bool, bool)` | `void` | Adds knockback/launch impulse or upward pogo bounce. |
| `RefillAirJumps(int?)` / `RefillAirDashes(int?, bool)` / `RefillClimbStamina()` | `void` | Refills air charges or stamina. |
| `SetVelocity(Vector3)` / `SetFacingDirection(Vector3)` / `CancelDash()` / `Reset()` | `void` | Direct overrides and full reset. |

---

### `CharacterController` (`sealed class`)

High-level composition of `CharacterMotor`, `CharacterActionRunner`, Core `StateMachine`, Core `Blackboard`, and optional `IEventBus`.

| Member | Type / Return | Description |
| --- | --- | --- |
| `Config`, `Motor`, `Actions`, `StateMachine`, `Blackboard`, `EventBus`, `Context` | properties | Access all underlying subsystems. |
| `Idle`, `Move`, `Airborne`, `Dash`, `WallSlide`, `WallClimb` | `StateBase<CharacterContext>` | Built-in locomotion HFSM states. |
| `CurrentState`, `Velocity`, `OnStateChanged` | properties / event | Active HFSM state, total velocity, and state transition callback. |
| `Tick(float, in CharacterInput, in MotorSensors)` / `FixedTick(float)` | `Vector3` / `void` | Advances actions, motor, Blackboard sync, and HFSM. |
| `RegisterAction(ICharacterAction)` / `TryStartAction(string, float)` / `Reset()` | `void` / `bool` | Action registration, buffered start, and full reset. |

---

### `ICharacterAction`, `SimpleCharacterAction` & `CharacterActionRunner`

- **`ICharacterAction` / `CharacterActionBase` / `DelegateCharacterAction`**: Interface, cooldown-backed base class, and delegate wrapper.
- **`SimpleCharacterAction`**: Data-driven `CharacterActionBase` supporting `ActionAirRule`, `CancelVelocityOnStart`, facing-aligned `LaunchImpulse`, and `onStart`/`onEnd` hooks.
- **`CharacterActionRunner`**: Manages registered actions, cooldowns, priority interruption (`CanBeInterruptedBy`), and buffered triggers (`Register`, `Unregister`, `TryStart`, `RequestAction`, `CancelActive`, `Tick`, `ClearBuffer`).

---

## Namespace: `ContentContent.Character.Unity`

### `CharacterConfigAsset` & `CharacterActionAsset` (`ScriptableObject`)

- **`CharacterConfigAsset`**: Reusable config preset asset (`Config`, `ApplyPreset(CharacterPresetType)`, `CreateRuntimeCopy()`).
- **`CharacterActionAsset`**: Reusable action asset (`ActionId`, `Duration`, `Cooldown`, `Priority`, `LockFlags`, `AirRule`, `CancelVelocityOnStart`, `LaunchImpulse`, `BuildRuntimeAction(...)`).

### `CharacterMotor2D` (`MonoBehaviour`)

Bridges `Rigidbody2D` (`SideScroller2D` / `TopDown2D`) to `CharacterController` in `FixedUpdate()`.

| Member | Type / Return | Description |
| --- | --- | --- |
| `Controller`, `Motor`, `Velocity`, `FacingDirection`, `IsGrounded`, `CurrentStateName` | properties | Access pure-C# controller, velocity, facing, and HFSM state. |
| `OnJumped`, `OnWallJumped`, `OnLanded`, `OnDashed`, `OnFacingChanged`, `OnStateChanged` | `UnityEvent` | Inspector-wireable audio, VFX, and UI events. |
| `AutoFitSensorsFromCollider()` / `ApplyPreset(CharacterPresetType)` | `bool` / `void` | Sizes 2D probes to attached `Collider2D` or swaps presets. |
| `SetMoveInput(Vector2)`, `PressJump()`, `ReleaseJump()`, `PressDash()`, `SetGrabHeld(bool)`, `PressDropThrough()` | `void` | Programmatic or custom input entry points. |
| `TriggerAction(string, float)` / `ApplyImpulse(Vector2)` / `ApplyBounce(float, bool, bool)` | `bool` / `void` | Action triggering, knockback, and pogo bounce. |
| `RefillAirJumps(int?)` / `RefillAirDashes(int?, bool)` | `void` | Charge restoration helpers. |

### `CharacterMotor3D` (`MonoBehaviour`)

Bridges Unity `CharacterController` (`ThirdPerson3D`) to `CharacterController` in `Update()` (`AutoFitSensorsFromController()`, `ApplyPreset`, `SetMoveInput(Vector3/Vector2)`, `PressJump`, `ReleaseJump`, `PressDash`, `TriggerAction`, `ApplyImpulse`, `UnityEvent`s).

### `CharacterAnimatorBridge`, `MovingPlatform2D` & `ISurfaceVelocityProvider`

- **`CharacterAnimatorBridge`**: Auto-links sibling motor + `Animator` + `SpriteRenderer`, drives optional Animator parameters cleanly without missing-parameter warnings, and flips 2D visuals on turn.
- **`MovingPlatform2D`**: Implements `ISurfaceVelocityProvider` with optional two-point patrol (`ConfigurePatrol`) or passive transform-delta velocity tracking.

---

## Namespace: `ContentContent.Character.Unity.Input`

Available automatically whenever `com.unity.inputsystem` is installed (`CONTENTCONTENT_INPUT_SYSTEM`):
- **`CharacterInput2D`**: Zero-setup New Input System bridge for `CharacterMotor2D` with optional `InputActionReference` overrides, `ActionInputBinding` triggers, and instant-play keyboard/gamepad defaults.
- **`CharacterInput3D`**: Zero-setup New Input System bridge for `CharacterMotor3D` with automatic camera-relative XZ movement and instant-play defaults.
