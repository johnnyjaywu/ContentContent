# Character Design & Best Practices

Game engines give you physics solvers (`Rigidbody2D`, `CharacterController`), not responsive characters. Raw physics produces floaty jumps, missed edge presses, accidental head-bonks, and stiff velocity fighting. `com.contentcontent.character` teaches and packages the industry-standard architecture used to make 2D and 3D characters feel crisp.

---

## 1. The Core Idea: Sensor-In / Velocity-Out

Most beginner character scripts fail because they tangle four unrelated jobs into one giant `MonoBehaviour`:
1. Reading raw keyboard/gamepad buttons
2. Raycasting the environment
3. Deciding jump rules, timers, and state transitions
4. Directly calling `Rigidbody2D` or `Animator`

Instead, `CharacterMotor` uses a **Sensor-In / Velocity-Out** pipeline:

```text
  [CharacterInput2D] ──► CharacterInput (intent) ──┐
                                                   ├──► CharacterMotor.Tick(dt) ──► Velocity + Events
  [CharacterMotor2D] ──► MotorSensors (contacts) ──┘          │
       (Physics2D)                                            ▼
                                                     Apply to Rigidbody2D /
                                                     Unity CharacterController
```

### Why This Design Matters
- **Testable in milliseconds:** Because `CharacterMotor` and `CharacterController` have zero `UnityEngine` references, every jump height, coyote window, wall kick, and cooldown runs deterministically in `dotnet test` without launching a scene.
- **Input-agnostic:** The motor only cares about intent (`JumpPressed`, `Move`). You can swap `CharacterInput2D` for an AI controller, replay file, or network packet without changing movement physics.
- **Portable across engines:**
  - **Unity:** `CharacterMotor2D` probes `Physics2D` and writes `Rigidbody2D.linearVelocity`; `CharacterMotor3D` feeds `CharacterController.Move`.
  - **Godot:** Feed `is_on_floor()`, `is_on_wall()`, and `get_wall_normal()` into `MotorSensors`, then assign the returned velocity to `CharacterBody2D.Velocity` / `CharacterBody3D.Velocity` and call `MoveAndSlide()`.
  - **Unreal:** The exact same sensor/intent separation mirrors `UCharacterMovementComponent::CalcVelocity()` and `PhysFalling()`, cleanly separating gameplay tuning from collision sweeps.

---

## 2. Game-Feel Primitives Explained

Great platformers feel fair because they quietly bridge the gap between what the player *meant* to do and the exact frame the physics engine checked:

- **Coyote Time:** Players often press Jump 1–3 frames after walking off a ledge. `CoyoteTime` keeps grounded jump eligibility alive briefly after losing ground contact.
- **Jump Buffering (with Tap Preservation):** Pressing Jump 2–4 frames *before* touching down remembers the press and jumps on the exact frame of landing — and if the player already released the button before landing, the buffered jump still applies the variable-height `JumpCutMultiplier`.
- **Asymmetric Gravity (`FallGravityMultiplier` & `ApexVelocityThreshold`):** Real gravity accelerates equally up and down, which feels floaty. Slightly lowering gravity near the jump peak (`ApexGravityMultiplier`) while boosting gravity during descent makes jumps readable yet snappy.
- **Ceiling Corner Correction:** Clipping the outermost 1–2 pixels of an overhead block normally stops a jump dead. `CharacterMotor2D` probes left and right edges and gently nudges the character around the corner when only one edge clips.
- **Moving Platform Momentum Without Double-Adding:** `CharacterMotor` tracks `LocomotionVelocity` separately from `CarriedPlatformVelocity`. While grounded, both add cleanly; on jump takeoff, the carried velocity transfers once into decaying `ExternalVelocity` instead of compounding every frame.

---

## 3. HFSM vs. Action Lock Flags

A common trap in state machines is **state explosion**: creating `GroundedAttack`, `AirborneAttack`, `WallAttack`, `HitstunAir`, and `HitstunGround` states with dozens of fragile arrows between them.

`CharacterController` solves this by pairing two small tools:
1. **A compact Locomotion HFSM (`StateMachine`):** Tracks spatial movement states (`Idle`, `Move`, `Airborne`, `WallSlide`, `WallClimb`, `Dash`) and publishes them to a shared `Blackboard`.
2. **Prioritized Actions with `ActionLockFlags` (`CharacterActionRunner`):** Combat and utility moves (`CharacterActionAsset` / `SimpleCharacterAction`) run *alongside* locomotion, temporarily locking only the capabilities they need (`BlockMovement | BlockTurn | BlockJump`). Higher-priority actions (like `Hitstun`) interrupt lower-priority ones cleanly.

---

## 4. Best Practices Built Into the Package

- **Never put physics integration in variable `Update()`:** `CharacterMotor2D` simulates in `FixedUpdate()` with `Rigidbody2D` interpolation enabled and `gravityScale = 0`, preventing frame-rate-dependent jump heights.
- **Use zero-friction materials on 2D character colliders:** Let `CharacterMotor` handle acceleration and wall-slide clamping instead of Unity's surface friction snagging against vertical walls.
- **Validate at the boundary:** `CharacterConfig.Sanitize()` clamps impossible negative values on load and edit, while `CharacterConfig.Validate()` warns designers immediately in the Inspector when two settings contradict each other.
