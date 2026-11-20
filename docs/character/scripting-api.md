# Character Scripting API

Complete reference for `com.contentcontent.character`:
- **Pure C# Runtime**: `ContentContent.Character`
- **Unity Adapters**: `ContentContent.Character.Unity`

Depends on `com.contentcontent.core` (`ContentContent.Core.Fsm`, `ContentContent.Core.Blackboards`, `ContentContent.Core.Timers`, `ContentContent.Core.Events`).

---

## Namespace: `ContentContent.Character`

### `MovementMode` (enum)

| Value | Description |
| --- | --- |
| `SideScroller2D` | 2D XY plane: horizontal movement on X, gravity & jump on Y, Z = 0. |
| `TopDown2D` | 2D XY plane: planar movement on X and Y, no gravity, Z = 0. |
| `ThirdPerson3D` | 3D XZ ground plane with gravity & jump on Y. |

### `ActionLockFlags` (`[Flags] enum`)

| Value | Description |
| --- | --- |
| `None` | No capabilities locked. |
| `BlockMovement` | Suppresses planar movement acceleration from input. |
| `BlockTurn` | Suppresses facing direction updates from movement input. |
| `BlockJump` | Suppresses jump execution (including buffered jumps). |
| `BlockDash` | Suppresses dash execution (including buffered dashes). |
| `SuspendGravity` | Suspends gravity integration while active. |
| `All` | Combines all flags above. |

---

### `CharacterConfig` (`[Serializable] class`)

| Field | Default | Description |
| --- | --- | --- |
| `Mode` | `SideScroller2D` | Spatial plane and gravity model. |
| `MaxSpeed` | `8f` | Maximum planar target speed. |
| `GroundAcceleration` / `GroundDeceleration` | `60f` / `70f` | Grounded acceleration and stop rates. |
| `AirAcceleration` / `AirDeceleration` | `35f` / `15f` | Airborne acceleration and stop rates. |
| `MinMoveThreshold` | `0.05f` | Minimum input/speed magnitude treated as movement. |
| `Gravity` | `30f` | Downward gravity magnitude along Y. |
| `FallGravityMultiplier` | `1.5f` | Extra gravity multiplier while falling (`Velocity.Y < 0`). |
| `MaxFallSpeed` | `25f` | Terminal downward fall speed. |
| `GroundStickVelocity` | `0.5f` | Small downward velocity while grounded. |
| `JumpVelocity` | `12f` | Upward jump launch speed. |
| `JumpCutMultiplier` | `0.5f` | Multiplier applied to upward velocity on early jump release. |
| `CoyoteTime` | `0.12f` | Post-ledge grounded jump grace window (seconds). |
| `JumpBufferTime` | `0.12f` | Pre-landing jump buffer window (seconds). |
| `MaxAirJumps` | `0` | Additional mid-air jumps before landing. |
| `DashSpeed` | `20f` | Dash burst speed. |
| `DashDuration` | `0.18f` | Dash burst duration (seconds). |
| `DashCooldown` | `0.6f` | Cooldown between dashes (seconds). |
| `DashBufferTime` | `0.1f` | Dash input buffer window (seconds). |
| `DashSuspendsGravity` | `true` | Zeroes vertical velocity & suspends gravity during dash. |
| `AllowAirDash` / `MaxAirDashes` | `true` / `1` | Mid-air dash permission and charge limit. |
| `KnockbackDecay` | `35f` | Decay rate for external impulses. |

Method: `Clone()` → `CharacterConfig`.

---

### `CharacterInput` & `MotorSensors` (`struct`)

- **`CharacterInput`**: `Move` (`System.Numerics.Vector3`), `JumpPressed`, `JumpHeld`, `JumpReleased`, `DashPressed`, static `None`.
- **`MotorSensors`**: `IsGrounded`, `GroundNormal`, `IsTouchingCeiling`, `IsTouchingWall`, `WallNormal`, static `Grounded`, static `Airborne`.

---

### `InputBuffer` (`sealed class`)

| Member | Return | Description |
| --- | --- | --- |
| `Count` | `int` | Number of currently buffered inputs. |
| `Buffer(string inputId, float windowSeconds)` | `void` | Buffers or refreshes an input for `windowSeconds`. |
| `HasBuffered(string inputId)` | `bool` | Returns `true` if unexpired. |
| `GetRemaining(string inputId)` | `float` | Remaining buffer window in seconds. |
| `Consume(string inputId)` | `bool` | Removes and returns `true` if buffered. |
| `Tick(float deltaTime)` | `void` | Advances timers and evicts expired entries. |
| `Clear()` | `void` | Clears all buffered entries. |

---

### `CharacterMotor` (`sealed class`)

```csharp
public CharacterMotor(
    CharacterConfig config = null,
    IEventBus eventBus = null,
    InputBuffer inputBuffer = null)
```

| Member | Type / Return | Description |
| --- | --- | --- |
| `Config`, `InputBuffer`, `Sensors` | — | Active config, input buffer, and latest sensor snapshot. |
| `Velocity`, `LocomotionVelocity`, `ExternalVelocity` | `Vector3` | Combined, locomotion-only, and external impulse velocities. |
| `FacingDirection` | `Vector3` | Normalized facing direction along the active plane. |
| `IsGrounded`, `IsDashing`, `IsDashCooldownReady` | `bool` | Core locomotion flags. |
| `PlanarSpeed`, `VerticalSpeed` | `float` | Decomposed speed scalars. |
| `DashTimeRemaining`, `DashCooldownRemaining`, `CoyoteTimeRemaining` | `float` | Active timer values. |
| `AirJumpsRemaining`, `AirDashesRemaining` | `int` | Remaining mid-air charges. |
| `ManualLocks` | `ActionLockFlags` | External lock mask OR'd with per-tick action locks. |
| `OnJumped`, `OnLanded`, `OnDashed` | `event Action<...>` | C# motor lifecycle events. |
| `Tick(float dt, in CharacterInput, in MotorSensors, ActionLockFlags = None)` | `Vector3` | Advances motor simulation and returns combined `Velocity`. |
| `ApplyImpulse(Vector3 impulse)` | `void` | Adds decaying planar knockback / vertical launch impulse. |
| `SetVelocity(Vector3 velocity)` | `void` | Overrides locomotion velocity directly. |
| `SetFacingDirection(Vector3 direction)` | `void` | Overrides facing direction along active plane. |
| `CancelDash()` / `Reset()` | `void` | Cancels dash or resets all motor state. |

---

### `ICharacterAction`, `CharacterActionBase`, `DelegateCharacterAction` & `CharacterActionRunner`

- **`ICharacterAction`**: `Id`, `Priority`, `LockFlags`, `IsFinished`, `CanStart(CharacterContext)`, `CanBeInterruptedBy(ICharacterAction)`, `TickCooldown(float)`, `OnStart(CharacterContext)`, `OnTick(CharacterContext, float)`, `OnEnd(CharacterContext, bool)`.
- **`CharacterActionBase`**: Abstract base with Core `Cooldown`, `Duration`, `Elapsed`, `Progress`, `CooldownRemaining`, `IsCooldownReady`, `Complete()`, `ResetCooldown()`, and virtual `HandleStart` / `HandleTick` / `HandleEnd`.
- **`DelegateCharacterAction`**: Delegate-backed `CharacterActionBase` for inline actions and tests.
- **`CharacterActionRunner`**:
  - `ActiveAction`, `IsActionActive`, `ActiveLockFlags`, `RegisteredCount`
  - `OnActionStarted`, `OnActionEnded`
  - `Register(ICharacterAction)`, `Unregister(string)`, `TryGetAction(string, out ICharacterAction)`
  - `TryStart(string, CharacterContext, float bufferWindow = 0f)`, `RequestAction(string, float bufferWindow = 0.12f)`
  - `CancelActive(CharacterContext)`, `Tick(CharacterContext, float)`, `ClearBuffer()`

---

### `CharacterController`, `CharacterContext`, `CharacterStates` & `CharacterBlackboardKeys`

```csharp
public CharacterController(
    CharacterConfig config = null,
    Blackboard blackboard = null,
    IEventBus eventBus = null)
```

| Member | Type / Return | Description |
| --- | --- | --- |
| `Config`, `Motor`, `Actions`, `StateMachine`, `Blackboard`, `EventBus`, `Context` | — | Composed subsystems. |
| `Idle`, `Move`, `Airborne`, `Dash` | `CharacterLocomotionState` | Built-in HFSM states (`StateBase<CharacterContext>`). |
| `CurrentState`, `Velocity` | `IState` / `Vector3` | Active HFSM state and current combined velocity. |
| `OnStateChanged` | `event Action<CharacterStateChangedEvent>` | Fired on HFSM state transitions. |
| `Tick(float dt, in CharacterInput, in MotorSensors)` | `Vector3` | Ticks actions, motor, Blackboard sync, and HFSM. |
| `FixedTick(float fixedDt)` | `void` | Forwards fixed-timestep tick to `StateMachine`. |
| `RegisterAction(ICharacterAction)` | `void` | Registers an action on `Actions`. |
| `TryStartAction(string actionId, float bufferWindow = 0f)` | `bool` | Attempts to start or buffer a registered action. |
| `Reset()` | `void` | Cancels active action, resets motor, returns HFSM to `Idle`. |

**`CharacterBlackboardKeys`** (auto-synced every tick):
`Velocity`, `Speed`, `PlanarSpeed`, `VerticalSpeed`, `FacingDirection`, `IsGrounded`, `IsDashing`, `AirJumpsRemaining`, `ActiveActionId`, `IsActionActive`.

**Struct Events (`CharacterEvents.cs`)**:
`CharacterJumpedEvent`, `CharacterLandedEvent`, `CharacterDashedEvent`, `CharacterActionStartedEvent`, `CharacterActionEndedEvent`, `CharacterStateChangedEvent`.

---

## Namespace: `ContentContent.Character.Unity`

- **`CharacterVectorExtensions`**: `ToNumerics(Vector3/Vector2)`, `ToUnity(System.Numerics.Vector3)`, `ToUnity2D(System.Numerics.Vector3)`.
- **`CharacterConfigAsset`** (`ScriptableObject`): Inspector preset with `Config` and `CreateRuntimeCopy()`.
- **`CharacterMotor2D`** (`MonoBehaviour`, requires `Rigidbody2D`, compiled when `CONTENTCONTENT_PHYSICS2D` is defined):
  - Properties: `Controller`, `Motor`, `Velocity`, `CurrentStateName`
  - Methods: `SetMoveInput(Vector2)`, `PressJump()`, `ReleaseJump()`, `PressDash()`, `TriggerAction(string, float = 0.12f)`, `ApplyImpulse(Vector2)`
- **`CharacterMotor3D`** (`MonoBehaviour`, requires `UnityEngine.CharacterController`, compiled when `CONTENTCONTENT_PHYSICS` is defined):
  - Properties: `Controller`, `Motor`, `Velocity`, `CurrentStateName`
  - Methods: `SetMoveInput(Vector3)`, `SetMoveInput(Vector2)`, `PressJump()`, `ReleaseJump()`, `PressDash()`, `TriggerAction(string, float = 0.12f)`, `ApplyImpulse(Vector3)`
