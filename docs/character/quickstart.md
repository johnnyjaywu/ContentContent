# Character Quickstart

Create a responsive 2D platformer, 2D top-down, or 3D third-person character in under a minute — with zero boilerplate input or glue code.

---

## 1. Install

`com.contentcontent.character` depends on `com.contentcontent.core`. Install both via **Window → Package Manager → + → Add package from git URL...**:

```text
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.core
https://github.com/johnnyjaywu/ContentContent.git?path=Packages/com.contentcontent.character
```

---

## 2. Create a Playable Character (Zero Code)

1. In Unity's top menu, choose **GameObject → ContentContent → Character →**:
   - **2D Side-Scroller Character**
   - **2D Top-Down Character**
   - **3D Third-Person Character**
2. Make sure there is a ground collider underneath it, press **Play**, and move immediately:
   - **Move:** `WASD` / `Arrow Keys` / Gamepad Left Stick
   - **Jump:** `Space` / Gamepad South (`S + Space` drops through one-way platforms)
   - **Dash:** `Left Shift` / Gamepad East
   - **Wall Grab (when enabled):** `E` / `Left Ctrl` / Gamepad Left Trigger

> **Tip:** Open **Character Playground** from **Window → Package Manager → ContentContent - Character → Downloadable Samples** to test presets, moving platforms, one-way drop-through, bounce pads, and wall jumps in a ready-made course.

---

## 3. Play With These Knobs

Select your character in the Inspector (or create a shared preset via **Right-click → Create → ContentContent → Character → Character Config**) and try tweaking feel while playing:

| Feel Goal | Settings to Adjust |
| --- | --- |
| **Snappy, non-floaty jumps** | Raise `Gravity` (`32–38`), raise `Fall Gravity Multiplier` (`1.5–1.8`), lower `Jump Release Cut` (`0.4–0.5`). |
| **Forgiving ledge jumps** | Increase `Coyote Time` (`0.10–0.14s`) and `Jump Buffer` (`0.10–0.15s`). |
| **Easier platforming hang time** | Set `Apex Hang Threshold` (`2–3`) and `Apex Gravity Multiplier` (`0.5`). |
| **Air mobility** | Switch preset to **Action** for double jump + air dash, or **Climber** for 8-way dash + stamina wall climbing. |
| **Tight stops vs. ice physics** | Raise `Ground Deceleration` for instant stops; lower it for momentum/skids. |

---

## 4. Add Moving Platforms, Animations & Actions (Still Zero Code)

- **Moving Platforms:** Add **`MovingPlatform2D`** to any platform collider (with a Kinematic `Rigidbody2D`). Adjust `Travel Offset` and `Speed` — your character automatically rides it and inherits launch momentum when jumping off.
- **Animations & Facing Flip:** Add **`CharacterAnimatorBridge`** to your character. It automatically flips 2D sprites on turn and drives any matching Animator parameters (`Speed`, `VerticalSpeed`, `IsGrounded`, `IsDashing`, `IsWallSliding`, `Jump`, `Dash`) without errors if some don't exist.
- **Custom Actions (Attack, Roll, Parry):**
  1. **Create → ContentContent → Character → Character Action** and set `Action Id` (e.g., `"Attack"`), `Duration`, `Cooldown`, and `Lock Flags`.
  2. Add it to **`Actions (Code-Free)`** on `CharacterMotor2D`/`3D` and hook audio, VFX, or hitboxes to `OnStarted` / `OnEnded`.
  3. Add a matching entry under **`Custom Action Bindings`** on `CharacterInput2D`/`3D`.
- **Audio / VFX Hooks:** Expand **`UnityEvents`** on the motor (`OnJumped`, `OnLanded`, `OnDashed`, `OnFacingChanged`, `OnStateChanged`) to trigger sound effects or particles directly in the Inspector.

---

## 5. Full C# Control Anytime

Every Inspector setting maps directly to open-source C# APIs:
- Call `_motor2D.TriggerAction("Attack")`, `_motor2D.ApplyBounce(14f)`, or `_motor2D.ApplyImpulse(knockback)` from combat scripts.
- Use `ContentContent.Character.CharacterController` or `CharacterMotor` directly in pure C# simulations and unit tests with zero `UnityEngine` dependencies.
