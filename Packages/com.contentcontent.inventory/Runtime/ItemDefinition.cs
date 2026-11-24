using System;
using System.Collections.Generic;

namespace ContentContent.Inventory
{
    /// <summary>
    /// Immutable item metadata registered in an <see cref="InventoryContainer"/>.
    /// </summary>
    public sealed class ItemDefinition
    {
        private static readonly string[] EmptyTags = Array.Empty<string>();
        private readonly string[] _tags;

        public string Id { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public int MaxStack { get; }
        public float UnitWeight { get; }
        public IReadOnlyList<string> Tags => _tags;

        public ItemDefinition(
            string id,
            string displayName = null,
            string category = "General",
            int maxStack = 99,
            float unitWeight = 0f,
            IEnumerable<string> tags = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ItemDefinition.Id must not be empty.", nameof(id));

            Id = id.Trim();
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName.Trim();
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
            MaxStack = Math.Max(1, maxStack);
            UnitWeight = Math.Max(0f, unitWeight);

            if (tags == null)
            {
                _tags = EmptyTags;
            }
            else
            {
                var list = new List<string>();
                foreach (string t in tags)
                {
                    if (!string.IsNullOrWhiteSpace(t))
                        list.Add(t.Trim());
                }
                _tags = list.Count > 0 ? list.ToArray() : EmptyTags;
            }
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || _tags.Length == 0)
                return false;

            string trimmed = tag.Trim();
            for (int i = 0; i < _tags.Length; i++)
            {
                if (string.Equals(_tags[i], trimmed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Value-type representation of a single inventory or equipment slot entry.
    /// </summary>
    public readonly struct ItemStack : IEquatable<ItemStack>
    {
        public static ItemStack Empty => default;

        public string ItemId { get; }
        public int Quantity { get; }
        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Quantity <= 0;

        public ItemStack(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            {
                ItemId = string.Empty;
                Quantity = 0;
            }
            else
            {
                ItemId = itemId;
                Quantity = quantity;
            }
        }

        public ItemStack WithQuantity(int quantity) => new ItemStack(ItemId, quantity);

        public bool Equals(ItemStack other) =>
            string.Equals(ItemId, other.ItemId, StringComparison.Ordinal) && Quantity == other.Quantity;

        public override bool Equals(object obj) => obj is ItemStack other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ItemId ?? string.Empty, Quantity);
    }

    public sealed class InventoryConfig
    {
        public int SlotCount = 20;
        public float MaxWeight = 0f;
        public bool AllowAutoStack = true;

        public void Sanitize()
        {
            SlotCount = Math.Max(1, SlotCount);
            MaxWeight = Math.Max(0f, MaxWeight);
        }
    }

    public readonly struct InventorySlotChangedEvent
    {
        public int SlotIndex { get; }
        public ItemStack Previous { get; }
        public ItemStack Current { get; }

        public InventorySlotChangedEvent(int slotIndex, ItemStack previous, ItemStack current)
        {
            SlotIndex = slotIndex;
            Previous = previous;
            Current = current;
        }
    }
}
