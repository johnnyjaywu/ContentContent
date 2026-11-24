# World & Progression Quickstart

Build interconnected worlds, validate against unreachable rooms and soft-locks right in EditMode, auto-reset enemies when rooms reload, and support dual-tier checkpoints (instant safe-ground recovery for pits vs. hard checkpoints) across platformers, action RPGs, dungeon crawlers, and adventure games.

---

## 1. Try the Sample Scene First

1. Ensure **`com.contentcontent.core`** is installed alongside `com.contentcontent.world`.
2. Open **Window → Package Manager**, select **ContentContent - World**, and click **Download** on **World Playground** under **Downloadable Samples**.
3. Click **Open Scene** and press **Play**.

---

## 2. Create & Validate Your World Map (`WorldGraphAsset`)

1. Right-click in the Project window: **Create → ContentContent → World → World Graph**.
2. Set **Start Room Id** (e.g., `Town`) and list your **Rooms** (`Town`, `Forest`, `BossArena`) along with outgoing **Exits**, **Required Abilities** (`WallJump`), **Required Flags**, and **Granted Abilities / Flags**.
3. The **World Graph Inspector** continuously runs **`WorldReachabilityValidator`**:
   - **Unreachable Rooms** catch broken one-way exit IDs or impossible ability requirements.
   - **Soft-Lock Rooms** catch rooms a player can drop into without being able to return to `StartRoomId` or any `hasCheckpoint` room.
   - Add temporary ability strings in the Inspector's **Assume Abilities** box to test mid-game progression states interactively.

---

## 3. Build Scene Portals, Checkpoints & Resets (Code-Free)

1. In the Hierarchy menu, click **GameObject → ContentContent → World → World Manager**, assign your **World Graph Asset** (optional) and Player **Transform**, and set **Initial Room Id**.
2. Add scene objects via **GameObject → ContentContent → World**:
   - **Room Spawn Point**: Marks landing anchors (`Room Id` + `Spawn Point Id`).
   - **Checkpoint 2D**: Activates on trigger touch by default (`Activate On Touch`) and exposes `On Activated`.
   - **Hazard Zone 2D**: Deals optional `IDamageable` damage and recovers back to the player's last safe ground (or hard checkpoint if fatal).
   - **Room Transition Trigger 2D**: Teleports the player between connected rooms and resets target room state.
3. Add **`Resettable Transform`** to doors, crates, boulders, or simple pickups so they automatically restore their starting position, rotation, scale, active state, and `Rigidbody2D` velocity whenever you enter the room or respawn at a checkpoint!

Each component Inspector validates scene setup (checking `Collider2D.isTrigger` and scene `WorldManagerBehaviour`) with 1-click **Fix** buttons.

---

## 4. Drive Transitions & Flags From Code (Full Control)

```csharp
using ContentContent.World.Unity;
using UnityEngine;

public class LeverSwitch : MonoBehaviour
{
    [SerializeField] private WorldManagerBehaviour _world;

    public void PullLever()
    {
        _world.Flags.SetBool("north_gate_open", true);
    }
}
```

Or use pure C# (`WorldGraph`, `WorldReachabilityValidator`, `RoomLifecycleController`, `CheckpointService`, `WorldFlagStore`) directly — see [Scripting API](scripting-api.md).

---

## Next Steps

- Learn *why* soft-lock validation and dual-tier checkpoints matter and how they map to Godot/Unreal in [Design & Best Practices](design.md).
- Browse all C# and Unity types in [Scripting API](scripting-api.md).
