# Save System Design & Best Practices

Most game tutorials start with `File.WriteAllText(path, JsonUtility.ToJson(data))`. That breaks in real shipping games for three reasons: a crash mid-write truncates the save file to 0 bytes, changing a field name in v1.2 silently wipes existing players' saves, and storing scene-object state inside one giant god-class couples every system together. `com.contentcontent.save` solves all three without third-party serializers.

---

## 1. Atomic Write & Backup Pipeline

```text
   ISaveable.CaptureState(section) ──► SaveDocument (vN) ──► SaveJsonCodec (Invariant RFC-8259)
                                                                       │
        Load Fallback ◄─── [slot.json.bak] ◄── Backup Copy ◄── [slot.json.tmp]
                                 │                                     │
                                 └────────── Atomic Replace ───────────▼
                                                                 [slot.json]
```

### Why This Pipeline Matters
- **Crash safety:** `FileSaveStorage` never writes directly over `[slot].json`. It writes and flushes `[slot].json.tmp`, copies the previous known-good `[slot].json` to `[slot].json.bak`, and moves `.tmp` into place. If `[slot].json` is truncated or unparseable, `SaveManager` automatically falls back to `[slot].json.bak`.
- **Zero-ambiguous types:** Standard JSON loses track of `int` vs `long` vs `float` vs `double`. `SaveValue` + `SaveJsonCodec` store explicit type tags cleanly using `CultureInfo.InvariantCulture` (preventing `,` vs `.` decimal bugs on European locales).
- **Safe slot IDs:** `SaveSlotUtility.ValidateSlotId` restricts slot IDs to `[a-zA-Z0-9_-]`, blocking directory traversal (`../`) from user-typed save names.

---

## 2. Schema Evolution via Sequential `ISaveMigration`

Instead of scattering `if (version < 2)` checks across every gameplay component, keep `SaveManager.CurrentSchemaVersion` explicit and register small pure `ISaveMigration` steps (`1 -> 2`, `2 -> 3`):
- Old saves load into a `SaveDocument`, step sequentially through matching migrations up to `CurrentSchemaVersion`, and then hand clean vN keys to `ISaveable.RestoreState`.

---

## 3. Best Practices & Common Pitfalls

- **Never reuse `SaveId` across two objects in a scene:** Use the Inspector's **Generate Unique ID** button or meaningful domain IDs like `"quest_journal"` and `"inventory_player"`.
- **Don't serialize live scene paths or memory pointers:** Store semantic IDs (room ID, item ID, quest ID, transform values) so refactoring hierarchy names doesn't break old saves.

---

## 4. Engine Transferability (Godot & Unreal)

- **Godot:** Layer 1 (`ContentContent.Save`) runs in Godot C# directly (`FileSaveStorage` pointed at `ProjectSettings.GlobalizePath("user://saves")`). Each node adds to a `"saveables"` group instead of inheriting `SaveableBehaviour`.
- **Unreal:** Mirrors Unreal's `USaveGame` + `ISaveable` participant pattern paired with `SaveManager` atomic swap semantics and deterministic post-load schema upgraders.
