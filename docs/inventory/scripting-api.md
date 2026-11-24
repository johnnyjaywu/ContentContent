# Inventory Scripting API

Complete reference for `ContentContent.Inventory` (pure C# core) and `ContentContent.Inventory.Unity` (Unity adapters).

---

## Namespace: `ContentContent.Inventory`

### `ItemDefinition`, `ItemStack`, `InventoryConfig`

- **`ItemDefinition`**: `Id`, `DisplayName`, `Category`, `MaxStack`, `UnitWeight`, `Tags`, `HasTag(string)`.
- **`ItemStack`** (`readonly struct`): `Empty`, `ItemId`, `Quantity`, `IsEmpty`, `WithQuantity(int)`.
- **`InventoryConfig`**: `SlotCount`, `MaxWeight` (`0` = unlimited), `AllowAutoStack`, `Sanitize()`.

### `InventoryContainer` (`sealed class`)

- Properties: `Config`, `SlotCount`, `MaxWeight`, `CurrentWeight`, `UsedSlotCount`, `FreeSlotCount`, `Slots`.
- Methods: `RegisterDefinition`, `TryGetDefinition`, `GetMaxStack`, `GetUnitWeight`, `GetSlot`, `CountOf`, `Has`, `CanAdd`, `TryAdd`, `TryAddAll`, `TryRemove`, `TryRemoveAt`, `TryMoveOrMerge`, `TrySplitStack`, `TryTransferTo`, `SortById`, `Clear`, `ExportSnapshot`, `ImportSnapshot`.
- Events: `OnSlotChanged`, `OnItemAdded`, `OnItemRemoved`, `OnChanged`.

### `EquipmentSlotRule` & `EquipmentSet`

- `DefineSlot(slotName, requiredCategory = null, requiredTag = null)`
- `HasSlot`, `GetEquipped`, `CanEquip`, `TryEquip`, `TryUnequip`, `TryEquipFromInventory`, `TryUnequipToInventory`
- Events: `OnEquipped`, `OnUnequipped`.

---

## Namespace: `ContentContent.Inventory.Unity`

- **`ItemDefinitionAsset` & `ItemCatalogAsset`**: ScriptableObject assets defining item properties and shared catalogs.
- **`InventoryComponent`** (`MonoBehaviour`): Hosts `InventoryContainer`, registers catalog items, populates `_startingItems`, exposes `AddItem`, `RemoveItem`, `AddItemById`, `RemoveItemById`, `HasItem`, `ClearInventory`, `OnItemAdded`, `OnItemRemoved`, `OnInventoryChanged`.
- **`EquipmentComponent`** (`MonoBehaviour`): Hosts `EquipmentSet`, auto-wires sibling `InventoryComponent`, defines `_slotRules`, exposes `TryEquipFromInventory`, `TryUnequipToInventory`, `OnEquipped`, `OnUnequipped`.
