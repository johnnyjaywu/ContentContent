using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Inventory.Unity
{
    /// <summary>
    /// Shared <see cref="ScriptableObject"/> catalog registering multiple <see cref="ItemDefinitionAsset"/>
    /// definitions into an <see cref="InventoryComponent"/> in one assignment.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "ContentContent/Inventory/Item Catalog", order = 1)]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/inventory/quickstart")]
    public class ItemCatalogAsset : ScriptableObject
    {
        [Tooltip("ItemDefinitionAssets registered automatically whenever an inventory initializes.")]
        [SerializeField] private List<ItemDefinitionAsset> _items = new List<ItemDefinitionAsset>();

        public IReadOnlyList<ItemDefinitionAsset> Items => _items;

        public void RegisterAll(InventoryContainer container)
        {
            if (container == null || _items == null)
                return;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null)
                    container.RegisterDefinition(_items[i].ToDefinition());
            }
        }

        public ItemDefinitionAsset FindById(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || _items == null)
                return null;

            string trimmed = itemId.Trim();
            for (int i = 0; i < _items.Count; i++)
            {
                ItemDefinitionAsset item = _items[i];
                if (item != null && string.Equals(item.ItemId, trimmed, StringComparison.Ordinal))
                    return item;
            }
            return null;
        }
    }
}
