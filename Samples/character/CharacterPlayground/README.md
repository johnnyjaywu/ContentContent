# Character Playground Sample

Real Physics2D interactive course built with `com.contentcontent.character`'s out-of-the-box Unity components:

- **`CharacterMotor2D` + `CharacterInput2D`** — Zero-boilerplate 2D locomotion with live preset switching (`Side-Scroller`, `ActionPlatformer`, `PrecisionClimber`, `TopDown`).
- **Real Level Obstacles** — Solid floor, ledge, wall-slide/climb pillars, one-way platform (`PlatformEffector2D` with `S + Space` drop-through), orange bounce pad (`ApplyBounce`), and **`MovingPlatform2D`** patrol platform with jump momentum inheritance.
- **Custom Action (`SimpleCharacterAction`)** — `"Attack"` action (`F` key) demonstrating cooldowns and `ActionLockFlags`.

## Setup

1. Download **Character Playground** from **Window → Package Manager → ContentContent - Character → Downloadable Samples** (with `com.contentcontent.core` installed).
2. Click **Open Scene** (or open `CharacterPlayground.unity`) and press **Play**.
3. Select the spawned **Player** object in the Hierarchy while playing to watch live **PlayMode Telemetry** and tweak feel sliders in real time.
4. Controls: **WASD / Arrow Keys** (Move), **Space** (Jump), **Left Shift** (Dash), **E / Left Ctrl** (Wall Grab in Climber preset), **S + Space** on green platform (Drop Through), **F** (Attack).
