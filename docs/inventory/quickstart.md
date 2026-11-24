# Inventory Quickstart

Build slot-based stackable inventories, weight-limited backpacks, and category/tag-filtered equipment slots without writing manual array juggling.

---

## 1. Try the Sample Scene First

1. Ensure **`com.contentcontent.core`** is installed alongside `com.contentcontent.inventory`.
2. Open **Window → Package Manager**, select **ContentContent - Inventory**, and click **Download** under **Downloadable Samples**.
3. Click **Open Scene** (`InventoryPlayground.unity`) and press **Play**.

---

## 2. Create Items & Character Inventory (Code-Free)

1. Right-click in the Project window: **Create → ContentContent → Inventory → Item Definition** to define items (`Item Id`, `Category`, `Max Stack`, `Unit Weight`, `Tags`).
2. Bundle them in a **Create → ContentContent → Inventory → Item Catalog**.
3. In Hierarchy, click **GameObject → ContentContent → Inventory → Equipped Character** to create a pre-wired `InventoryComponent` + `EquipmentComponent` pair.
   - Assign your **Item Catalog** on `InventoryComponent` and optionally list **Starting Items**.
   - Wire `InteractableBehaviour.OnInteracted` directly to `InventoryComponent.AddItem(ItemDefinitionAsset)` for zero-code item pickups!
   - Inspect the live PlayMode slots, weight bar, **Sort**, and **Unequip** controls directly in the Inspector.

---

## 3. Code Usage

```csharp
using ContentContent.Inventory;

var bag = new InventoryContainer(new InventoryConfig { SlotCount = 12, MaxWeight = 40f });
bag.RegisterDefinition(new ItemDefinition("potion", "Health Potion", "Consumable", maxStack: 10, unitWeight: 0.5f));
bag.TryAddAll("potion", 3);
```

---

## Next Steps

- Learn *why* atomic operations and ID-based stacks beat `List<ItemAsset>` in [Design & Best Practices](design.md).
- Browse all C# and Unity types in [Scripting API](scripting-api.md).
