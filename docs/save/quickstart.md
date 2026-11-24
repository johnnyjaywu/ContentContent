# Save System Quickstart

Persist player progress safely with atomic slot writes, automatic backup fallback, schema version migrations, and zero-code `SaveableTransform` objects.

---

## 1. Try the Sample Scene First

1. Ensure **`com.contentcontent.core`** is installed alongside `com.contentcontent.save`.
2. Open **Window → Package Manager**, select **ContentContent - Save**, and click **Download** under **Downloadable Samples**.
3. Click **Open Scene** (`SavePlayground.unity`) and press **Play**.

---

## 2. Set Up Saving & Loading Without Code

1. In the Hierarchy menu, click **GameObject → ContentContent → Save → Save Manager**.
   - Configure **Schema Version** (`1` initially), **Default Slot Id** (`slot_0`), and **Auto Save Interval Seconds** (optional timed autosave).
   - Wire UI Buttons directly to `SaveManagerBehaviour.SaveDefaultSlot()`, `LoadDefaultSlot()`, or `SaveAutosave()`.
2. Add **`SaveableTransform`** (or **GameObject → ContentContent → Save → Saveable Transform**) to doors, crates, collectibles, or the player.
   - Each `SaveableBehaviour` Inspector checks for empty or duplicate **Save Id** values and gives you a 1-click **Generate Unique ID** fix button.
   - Deactivated objects (such as collected pickups) remain registered so loading restores `activeSelf = true` cleanly.

---

## 3. Save Custom Gameplay Scripts in Code

Subclass `SaveableBehaviour` (or implement pure C# `ISaveable`):

```csharp
using ContentContent.Save;
using ContentContent.Save.Unity;

public class PlayerWalletSaveable : SaveableBehaviour
{
    public int gold = 50;

    public override void CaptureState(SaveSection section) => section.SetInt("gold", gold);

    public override void RestoreState(SaveSection section) => gold = section.GetInt("gold", 50);
}
```

---

## Next Steps

- Learn *why* atomic `.tmp`/`.bak` writes and version migrations prevent corrupted player saves in [Design & Best Practices](design.md).
- Browse all C# and Unity types in [Scripting API](scripting-api.md).
