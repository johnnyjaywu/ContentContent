using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Inventory.Unity
{
    /// <summary>
    /// Code-free <see cref="MonoBehaviour"/> hosting an <see cref="InventoryContainer"/>
    /// with ScriptableObject catalog registration, weight limits, starting items, and Inspector UnityEvents.
    /// </summary>
    [AddComponentMenu("ContentContent/Inventory/Inventory Component")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/inventory/quickstart")]
    public class InventoryComponent : MonoBehaviour
    {
        [Serializable]
        public struct StartingItemEntry
        {
            [Tooltip("Optional ItemDefinitionAsset (registers its metadata automatically).")]
            public ItemDefinitionAsset item;

            [Tooltip("Item ID used when Item is omitted.")]
            public string itemIdOverride;

            [Min(1)]
            public int quantity;
        }

        [Header("Configuration")]
        [Tooltip("Optional ItemCatalogAsset defining stack caps, categories, weights, and tags.")]
        [SerializeField] private ItemCatalogAsset _catalog;

        [Tooltip("Total inventory slots.")]
        [SerializeField, Min(1)] private int _slotCount = 20;

        [Tooltip("Carry weight limit (0 = unlimited weight).")]
        [SerializeField, Min(0f)] private float _maxWeight;

        [Tooltip("Automatically merges added items into matching partial stacks first.")]
        [SerializeField] private bool _allowAutoStack = true;

        [Tooltip("Items placed into the inventory when it initializes.")]
        [SerializeField] private List<StartingItemEntry> _startingItems = new List<StartingItemEntry>();

        [Header("Events")]
        [SerializeField] private UnityEvent<string, int> _onItemAdded = new UnityEvent<string, int>();
        [SerializeField] private UnityEvent<string, int> _onItemRemoved = new UnityEvent<string, int>();
        [SerializeField] private UnityEvent _onInventoryChanged = new UnityEvent();

        private InventoryContainer _container;

        public InventoryContainer Container
        {
            get
            {
                EnsureInitialized();
                return _container;
            }
        }

        public ItemCatalogAsset Catalog => _catalog;
        public UnityEvent<string, int> OnItemAdded => _onItemAdded ?? (_onItemAdded = new UnityEvent<string, int>());
        public UnityEvent<string, int> OnItemRemoved => _onItemRemoved ?? (_onItemRemoved = new UnityEvent<string, int>());
        public UnityEvent OnInventoryChanged => _onInventoryChanged ?? (_onInventoryChanged = new UnityEvent());

        private void OnValidate()
        {
            _slotCount = Mathf.Max(1, _slotCount);
            _maxWeight = Mathf.Max(0f, _maxWeight);
            if (_startingItems != null)
            {
                for (int i = 0; i < _startingItems.Count; i++)
                {
                    StartingItemEntry entry = _startingItems[i];
                    if (entry.quantity < 1)
                    {
                        entry.quantity = 1;
                        _startingItems[i] = entry;
                    }
                }
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (_container == null)
                return;

            _container.OnItemAdded -= HandleItemAdded;
            _container.OnItemRemoved -= HandleItemRemoved;
            _container.OnChanged -= HandleChanged;
        }

        public void RegisterItemAsset(ItemDefinitionAsset itemAsset)
        {
            if (itemAsset != null)
                Container.RegisterDefinition(itemAsset.ToDefinition());
        }

        public void AddItem(ItemDefinitionAsset itemAsset)
        {
            if (itemAsset == null)
                return;

            Container.RegisterDefinition(itemAsset.ToDefinition());
            Container.TryAdd(itemAsset.ItemId, 1, out _, out _);
        }

        public void RemoveItem(ItemDefinitionAsset itemAsset)
        {
            if (itemAsset == null)
                return;

            Container.TryRemove(itemAsset.ItemId, 1);
        }

        public bool AddItemById(string itemId, int quantity = 1) =>
            Container.TryAddAll(itemId, quantity);

        public bool RemoveItemById(string itemId, int quantity = 1) =>
            Container.TryRemove(itemId, quantity);

        public bool HasItem(string itemId, int quantity = 1) =>
            Container.Has(itemId, quantity);

        public void ClearInventory() => Container.Clear();

        private void EnsureInitialized()
        {
            if (_container != null)
                return;

            var config = new InventoryConfig
            {
                SlotCount = _slotCount,
                MaxWeight = _maxWeight,
                AllowAutoStack = _allowAutoStack
            };

            _container = new InventoryContainer(config);
            if (_catalog != null)
                _catalog.RegisterAll(_container);

            _container.OnItemAdded += HandleItemAdded;
            _container.OnItemRemoved += HandleItemRemoved;
            _container.OnChanged += HandleChanged;

            if (_startingItems != null)
            {
                for (int i = 0; i < _startingItems.Count; i++)
                {
                    StartingItemEntry entry = _startingItems[i];
                    string id = entry.item != null ? entry.item.ItemId : entry.itemIdOverride;
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    if (entry.item != null)
                        _container.RegisterDefinition(entry.item.ToDefinition());

                    _container.TryAdd(id.Trim(), Mathf.Max(1, entry.quantity), out _, out _);
                }
            }
        }

        private void HandleItemAdded(string itemId, int qty) => _onItemAdded?.Invoke(itemId, qty);
        private void HandleItemRemoved(string itemId, int qty) => _onItemRemoved?.Invoke(itemId, qty);
        private void HandleChanged() => _onInventoryChanged?.Invoke();
    }
}
