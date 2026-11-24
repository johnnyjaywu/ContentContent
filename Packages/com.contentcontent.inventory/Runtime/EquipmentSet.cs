using System;
using System.Collections.Generic;

namespace ContentContent.Inventory
{
    public sealed class EquipmentSlotRule
    {
        public string SlotName { get; }
        public string RequiredCategory { get; }
        public string RequiredTag { get; }

        public EquipmentSlotRule(string slotName, string requiredCategory = null, string requiredTag = null)
        {
            if (string.IsNullOrWhiteSpace(slotName))
                throw new ArgumentException("Equipment slot name must not be empty.", nameof(slotName));

            SlotName = slotName.Trim();
            RequiredCategory = string.IsNullOrWhiteSpace(requiredCategory) ? string.Empty : requiredCategory.Trim();
            RequiredTag = string.IsNullOrWhiteSpace(requiredTag) ? string.Empty : requiredTag.Trim();
        }

        public bool Accepts(ItemDefinition definition, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            if (!string.IsNullOrEmpty(RequiredCategory))
            {
                if (definition == null || !string.Equals(definition.Category, RequiredCategory, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrEmpty(RequiredTag))
            {
                if (definition == null || !definition.HasTag(RequiredTag))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Named equipment slots with category/tag eligibility rules and atomic swaps with an <see cref="InventoryContainer"/>.
    /// </summary>
    public sealed class EquipmentSet
    {
        private readonly Dictionary<string, EquipmentSlotRule> _rules = new Dictionary<string, EquipmentSlotRule>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ItemStack> _equipped = new Dictionary<string, ItemStack>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _slotOrder = new List<string>();

        public IReadOnlyList<string> SlotNames => _slotOrder;
        public IReadOnlyDictionary<string, ItemStack> EquippedSlots => _equipped;

        public event Action<string, ItemStack> OnEquipped;
        public event Action<string, ItemStack> OnUnequipped;

        public void DefineSlot(string slotName, string requiredCategory = null, string requiredTag = null)
        {
            var rule = new EquipmentSlotRule(slotName, requiredCategory, requiredTag);
            if (!_rules.ContainsKey(rule.SlotName))
            {
                _slotOrder.Add(rule.SlotName);
                _equipped[rule.SlotName] = ItemStack.Empty;
            }
            _rules[rule.SlotName] = rule;
        }

        public bool HasSlot(string slotName) => !string.IsNullOrEmpty(slotName) && _rules.ContainsKey(slotName);

        public ItemStack GetEquipped(string slotName)
        {
            if (string.IsNullOrEmpty(slotName) || !_equipped.TryGetValue(slotName, out ItemStack stack))
                return ItemStack.Empty;
            return stack;
        }

        public bool CanEquip(string slotName, string itemId, ItemDefinition definition = null)
        {
            if (string.IsNullOrEmpty(slotName) || !_rules.TryGetValue(slotName, out EquipmentSlotRule rule))
                return false;

            return rule.Accepts(definition, itemId);
        }

        public bool TryEquip(string slotName, string itemId, out ItemStack previouslyEquipped, ItemDefinition definition = null)
        {
            previouslyEquipped = ItemStack.Empty;
            if (!CanEquip(slotName, itemId, definition))
                return false;

            EquipmentSlotRule rule = _rules[slotName];
            previouslyEquipped = _equipped[rule.SlotName];
            var next = new ItemStack(itemId.Trim(), 1);
            _equipped[rule.SlotName] = next;

            if (!previouslyEquipped.IsEmpty)
                OnUnequipped?.Invoke(rule.SlotName, previouslyEquipped);

            OnEquipped?.Invoke(rule.SlotName, next);
            return true;
        }

        public bool TryUnequip(string slotName, out ItemStack unequipped)
        {
            unequipped = ItemStack.Empty;
            if (string.IsNullOrEmpty(slotName) || !_rules.TryGetValue(slotName, out EquipmentSlotRule rule))
                return false;

            ItemStack current = _equipped[rule.SlotName];
            if (current.IsEmpty)
                return false;

            unequipped = current;
            _equipped[rule.SlotName] = ItemStack.Empty;
            OnUnequipped?.Invoke(rule.SlotName, current);
            return true;
        }

        public bool TryEquipFromInventory(string slotName, InventoryContainer inventory, int inventorySlotIndex)
        {
            if (inventory == null || inventorySlotIndex < 0 || inventorySlotIndex >= inventory.SlotCount)
                return false;

            ItemStack candidate = inventory.GetSlot(inventorySlotIndex);
            if (candidate.IsEmpty)
                return false;

            inventory.TryGetDefinition(candidate.ItemId, out ItemDefinition def);
            if (!CanEquip(slotName, candidate.ItemId, def))
                return false;

            EquipmentSlotRule rule = _rules[slotName];
            ItemStack prevEquipped = _equipped[rule.SlotName];

            // Take 1 unit out temporarily to validate whether prevEquipped can fit back atomically.
            if (!inventory.TryRemoveAt(inventorySlotIndex, 1, out _))
                return false;

            if (!prevEquipped.IsEmpty && !inventory.TryAddAll(prevEquipped.ItemId, prevEquipped.Quantity))
            {
                // Roll back item into inventory slot so nothing is lost when weight/slots reject the swap.
                inventory.RestoreExactSlot(inventorySlotIndex, candidate);
                return false;
            }

            var equippedStack = new ItemStack(candidate.ItemId, 1);
            _equipped[rule.SlotName] = equippedStack;

            if (!prevEquipped.IsEmpty)
                OnUnequipped?.Invoke(rule.SlotName, prevEquipped);
            OnEquipped?.Invoke(rule.SlotName, equippedStack);
            return true;
        }

        public bool TryUnequipToInventory(string slotName, InventoryContainer inventory)
        {
            if (inventory == null || string.IsNullOrEmpty(slotName) || !_rules.TryGetValue(slotName, out EquipmentSlotRule rule))
                return false;

            ItemStack current = _equipped[rule.SlotName];
            if (current.IsEmpty || !inventory.TryAddAll(current.ItemId, current.Quantity))
                return false;

            _equipped[rule.SlotName] = ItemStack.Empty;
            OnUnequipped?.Invoke(rule.SlotName, current);
            return true;
        }
    }
}
