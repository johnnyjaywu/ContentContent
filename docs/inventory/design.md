# Inventory Design & Best Practices

Storing player gear as raw `List<MonoBehaviour>` or `List<GameObject>` references breaks the moment a pickup is destroyed, a chest swaps items, or a save file loads. Conversely, naive slot lists lose items when swapping equipped gear into a full or over-weight bag. `com.contentcontent.inventory` solves both through lightweight value stacks and atomic operations.

---

## 1. ItemDefinition Metadata + Immutable Value Stacks

```text
   ItemCatalogAsset ──(Read-Only Definitions: MaxStack, Weight, Category, Tags)──┐
                                                                                 ▼
   EquipmentSet (Slot Rules) ◄─── Atomic Swap + Rollback ───► InventoryContainer (ItemStack[])
```

### Why This Architecture Matters
- **Lightweight `ItemStack` structs:** Slots store `(ItemId, Quantity)` structs rather than heavyweight object references, making UI refresh, network sync, and save snapshots (`ExportSnapshot`/`ImportSnapshot`) trivial.
- **Transactional atomicity:** `TryAddAll`, `TryRemove`, and `TryEquipFromInventory` check total room/weight up front — and `TryEquipFromInventory` rolls back the removed inventory stack if the swapped-out item cannot fit back into the bag.
- **Rule-driven gear slots:** `EquipmentSlotRule` filters by `RequiredCategory` (`Weapon`, `Armor`) and optional `RequiredTag` (`OneHanded`, `Heavy`), keeping UI and pickup code clean.

---

## 2. Best Practices & Common Pitfalls

- **Use `TryAddAll` for rewards/crafting and `TryAdd` for ground pickups:** Quest rewards and merchant purchases should succeed or fail atomically (`TryAddAll`), while ground loot can pick up partial stacks and leave `remainder` on the floor (`TryAdd`).
- **Keep `MaxStack = 1` for unique or durable gear** so equipment slots (`TryEquipFromInventory`) manage individual items cleanly.

---

## 3. Engine Transferability (Godot & Unreal)

- **Godot:** Layer 1 (`ContentContent.Inventory`) drops straight into Godot C#, paired with `Resource` `ItemDefinition` assets instead of `ScriptableObject`.
- **Unreal:** Mirrors `UPrimaryDataAsset` item definitions paired with a replicated `FItemStack` array container component and rule-checked equipment manager.
