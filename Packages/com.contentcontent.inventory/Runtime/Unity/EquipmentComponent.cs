using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Inventory.Unity
{
    /// <summary>
    /// Code-free <see cref="MonoBehaviour"/> hosting an <see cref="EquipmentSet"/> and automatically
    /// wiring to a sibling <see cref="InventoryComponent"/> for atomic item equip/unequip swaps.
    /// </summary>
    [AddComponentMenu("ContentContent/Inventory/Equipment Component")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/inventory/quickstart")]
    public class EquipmentComponent : MonoBehaviour
    {
        [Serializable]
        public struct SlotRuleEntry
        {
            [Tooltip("Equipment slot name (e.g., Weapon, Armor, Accessory).")]
            public string slotName;

            [Tooltip("Optional ItemDefinition category required (leave empty to allow any category).")]
            public string requiredCategory;

            [Tooltip("Optional ItemDefinition tag required (leave empty to allow any tags).")]
            public string requiredTag;
        }

        [Header("Links")]
        [Tooltip("Target InventoryComponent for equip/unequip swaps. Auto-detected on this GameObject if left empty.")]
        [SerializeField] private InventoryComponent _inventory;

        [Header("Slot Rules")]
        [SerializeField] private List<SlotRuleEntry> _slotRules = new List<SlotRuleEntry>
        {
            new SlotRuleEntry { slotName = "Weapon", requiredCategory = "Weapon", requiredTag = string.Empty },
            new SlotRuleEntry { slotName = "Armor", requiredCategory = "Armor", requiredTag = string.Empty }
        };

        [Header("Events")]
        [SerializeField] private UnityEvent<string, string> _onEquipped = new UnityEvent<string, string>();
        [SerializeField] private UnityEvent<string, string> _onUnequipped = new UnityEvent<string, string>();

        private EquipmentSet _equipment;

        public InventoryComponent Inventory
        {
            get
            {
                EnsureInitialized();
                return _inventory;
            }
        }

        public EquipmentSet Equipment
        {
            get
            {
                EnsureInitialized();
                return _equipment;
            }
        }

        public UnityEvent<string, string> OnEquipped => _onEquipped ?? (_onEquipped = new UnityEvent<string, string>());
        public UnityEvent<string, string> OnUnequipped => _onUnequipped ?? (_onUnequipped = new UnityEvent<string, string>());

        private void Reset()
        {
            if (_inventory == null)
                _inventory = GetComponent<InventoryComponent>();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (_equipment == null)
                return;

            _equipment.OnEquipped -= HandleEquipped;
            _equipment.OnUnequipped -= HandleUnequipped;
        }

        public bool TryEquipFromInventory(string slotName, int inventorySlotIndex)
        {
            EnsureInitialized();
            return _inventory != null && _equipment.TryEquipFromInventory(slotName, _inventory.Container, inventorySlotIndex);
        }

        public bool TryUnequipToInventory(string slotName)
        {
            EnsureInitialized();
            return _inventory != null && _equipment.TryUnequipToInventory(slotName, _inventory.Container);
        }

        private void EnsureInitialized()
        {
            if (_equipment != null)
                return;

            if (_inventory == null)
                _inventory = GetComponent<InventoryComponent>();

            _equipment = new EquipmentSet();
            if (_slotRules != null)
            {
                for (int i = 0; i < _slotRules.Count; i++)
                {
                    SlotRuleEntry rule = _slotRules[i];
                    if (!string.IsNullOrWhiteSpace(rule.slotName))
                        _equipment.DefineSlot(rule.slotName, rule.requiredCategory, rule.requiredTag);
                }
            }

            _equipment.OnEquipped += HandleEquipped;
            _equipment.OnUnequipped += HandleUnequipped;
        }

        private void HandleEquipped(string slotName, ItemStack stack) => _onEquipped?.Invoke(slotName, stack.ItemId);
        private void HandleUnequipped(string slotName, ItemStack stack) => _onUnequipped?.Invoke(slotName, stack.ItemId);
    }
}
