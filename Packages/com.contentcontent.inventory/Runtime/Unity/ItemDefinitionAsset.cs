using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Inventory.Unity
{
    /// <summary>
    /// Designer-authored <see cref="ScriptableObject"/> defining an item type, stack cap,
    /// carry weight, category, and tags for zero-code inventory & equipment workflows.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "ContentContent/Inventory/Item Definition", order = 0)]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/inventory/quickstart")]
    public class ItemDefinitionAsset : ScriptableObject
    {
        [Tooltip("Unique identifier referenced by inventory snapshots, saves, and drop tables. Falls back to asset name.")]
        [SerializeField] private string _itemId = string.Empty;

        [Tooltip("Human-readable name shown in UI and Inspectors.")]
        [SerializeField] private string _displayName = string.Empty;

        [Tooltip("Category used for equipment filtering (e.g., Weapon, Armor, Consumable, Quest).")]
        [SerializeField] private string _category = "General";

        [Tooltip("Maximum stack size allowed per slot (1 = non-stackable gear).")]
        [SerializeField, Min(1)] private int _maxStack = 99;

        [Tooltip("Weight per unit used when Max Weight > 0 on InventoryComponent.")]
        [SerializeField, Min(0f)] private float _unitWeight;

        [Tooltip("Optional icon sprite displayed by game UI.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Optional gameplay tags matched by EquipmentSlotRules (e.g., TwoHanded, FireResistant).")]
        [SerializeField] private List<string> _tags = new List<string>();

        public string ItemId => string.IsNullOrWhiteSpace(_itemId) ? name : _itemId.Trim();
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? ItemId : _displayName.Trim();
        public string Category => string.IsNullOrWhiteSpace(_category) ? "General" : _category.Trim();
        public int MaxStack => Mathf.Max(1, _maxStack);
        public float UnitWeight => Mathf.Max(0f, _unitWeight);
        public Sprite Icon => _icon;
        public IReadOnlyList<string> Tags => _tags;

        private void OnValidate()
        {
            _maxStack = Mathf.Max(1, _maxStack);
            _unitWeight = Mathf.Max(0f, _unitWeight);
        }

        public ItemDefinition ToDefinition()
        {
            return new ItemDefinition(ItemId, DisplayName, Category, MaxStack, UnitWeight, _tags);
        }
    }
}
