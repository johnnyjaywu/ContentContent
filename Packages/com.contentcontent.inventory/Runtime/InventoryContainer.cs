using System;
using System.Collections.Generic;

namespace ContentContent.Inventory
{
    /// <summary>
    /// Deterministic slot-based inventory container enforcing stack caps, optional carry-weight
    /// limits, atomic all-or-nothing additions/removals, splitting, transfers, and snapshot serialization.
    /// </summary>
    public sealed class InventoryContainer
    {
        private const int DefaultUnregisteredMaxStack = 99;

        private readonly Dictionary<string, ItemDefinition> _definitions = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
        private ItemStack[] _slots;

        public InventoryConfig Config { get; }
        public int SlotCount => _slots.Length;
        public float MaxWeight => Config.MaxWeight;
        public IReadOnlyList<ItemStack> Slots => _slots;

        public float CurrentWeight
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < _slots.Length; i++)
                {
                    ItemStack stack = _slots[i];
                    if (stack.IsEmpty)
                        continue;

                    total += GetUnitWeight(stack.ItemId) * stack.Quantity;
                }
                return total;
            }
        }

        public int UsedSlotCount
        {
            get
            {
                int used = 0;
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (!_slots[i].IsEmpty)
                        used++;
                }
                return used;
            }
        }

        public int FreeSlotCount => _slots.Length - UsedSlotCount;

        public event Action<InventorySlotChangedEvent> OnSlotChanged;
        public event Action<string, int> OnItemAdded;
        public event Action<string, int> OnItemRemoved;
        public event Action OnChanged;

        public InventoryContainer(InventoryConfig config = null)
        {
            Config = config ?? new InventoryConfig();
            Config.Sanitize();
            _slots = new ItemStack[Config.SlotCount];
        }

        public void RegisterDefinition(ItemDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            _definitions[definition.Id] = definition;
        }

        public bool TryGetDefinition(string itemId, out ItemDefinition definition)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                definition = null;
                return false;
            }
            return _definitions.TryGetValue(itemId, out definition);
        }

        public int GetMaxStack(string itemId) =>
            TryGetDefinition(itemId, out ItemDefinition def) ? def.MaxStack : DefaultUnregisteredMaxStack;

        public float GetUnitWeight(string itemId) =>
            TryGetDefinition(itemId, out ItemDefinition def) ? def.UnitWeight : 0f;

        public ItemStack GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _slots[index];
        }

        public int CountOf(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;

            int count = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty && string.Equals(_slots[i].ItemId, itemId, StringComparison.Ordinal))
                    count += _slots[i].Quantity;
            }
            return count;
        }

        public bool Has(string itemId, int quantity = 1) =>
            quantity > 0 && CountOf(itemId) >= quantity;

        public bool CanAdd(string itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
                return false;

            string trimmed = itemId.Trim();
            int addable = CalculateMaxAddableQuantity(trimmed, quantity);
            return addable >= quantity;
        }

        public bool TryAdd(string itemId, int quantity, out int added, out int remainder)
        {
            added = 0;
            remainder = Math.Max(0, quantity);
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
                return false;

            string trimmed = itemId.Trim();
            int allowed = CalculateMaxAddableQuantity(trimmed, quantity);
            if (allowed <= 0)
                return false;

            int maxStack = GetMaxStack(trimmed);
            int remainingToPlace = allowed;

            if (Config.AllowAutoStack)
            {
                for (int i = 0; i < _slots.Length && remainingToPlace > 0; i++)
                {
                    ItemStack s = _slots[i];
                    if (s.IsEmpty || !string.Equals(s.ItemId, trimmed, StringComparison.Ordinal))
                        continue;

                    int room = maxStack - s.Quantity;
                    if (room <= 0)
                        continue;

                    int delta = Math.Min(room, remainingToPlace);
                    SetSlotInternal(i, new ItemStack(trimmed, s.Quantity + delta));
                    remainingToPlace -= delta;
                }
            }

            for (int i = 0; i < _slots.Length && remainingToPlace > 0; i++)
            {
                if (!_slots[i].IsEmpty)
                    continue;

                int delta = Math.Min(maxStack, remainingToPlace);
                SetSlotInternal(i, new ItemStack(trimmed, delta));
                remainingToPlace -= delta;
            }

            added = allowed - remainingToPlace;
            remainder = quantity - added;

            if (added > 0)
            {
                OnItemAdded?.Invoke(trimmed, added);
                OnChanged?.Invoke();
            }

            return added > 0;
        }

        public bool TryAddAll(string itemId, int quantity)
        {
            if (!CanAdd(itemId, quantity))
                return false;

            TryAdd(itemId, quantity, out int added, out _);
            return added == quantity;
        }

        public bool TryRemove(string itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
                return false;

            string trimmed = itemId.Trim();
            if (CountOf(trimmed) < quantity)
                return false;

            int remaining = quantity;
            for (int i = _slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                ItemStack s = _slots[i];
                if (s.IsEmpty || !string.Equals(s.ItemId, trimmed, StringComparison.Ordinal))
                    continue;

                int take = Math.Min(s.Quantity, remaining);
                int left = s.Quantity - take;
                SetSlotInternal(i, left > 0 ? new ItemStack(trimmed, left) : ItemStack.Empty);
                remaining -= take;
            }

            OnItemRemoved?.Invoke(trimmed, quantity);
            OnChanged?.Invoke();
            return true;
        }

        public bool TryRemoveAt(int slotIndex, int quantity, out ItemStack removed)
        {
            removed = ItemStack.Empty;
            if (slotIndex < 0 || slotIndex >= _slots.Length || quantity <= 0)
                return false;

            ItemStack current = _slots[slotIndex];
            if (current.IsEmpty)
                return false;

            int take = Math.Min(current.Quantity, quantity);
            removed = new ItemStack(current.ItemId, take);

            int left = current.Quantity - take;
            SetSlotInternal(slotIndex, left > 0 ? new ItemStack(current.ItemId, left) : ItemStack.Empty);

            OnItemRemoved?.Invoke(current.ItemId, take);
            OnChanged?.Invoke();
            return true;
        }

        public bool TryMoveOrMerge(int fromSlot, int toSlot)
        {
            if (fromSlot < 0 || fromSlot >= _slots.Length || toSlot < 0 || toSlot >= _slots.Length || fromSlot == toSlot)
                return false;

            ItemStack src = _slots[fromSlot];
            if (src.IsEmpty)
                return false;

            ItemStack dst = _slots[toSlot];
            if (dst.IsEmpty)
            {
                SetSlotInternal(toSlot, src);
                SetSlotInternal(fromSlot, ItemStack.Empty);
                OnChanged?.Invoke();
                return true;
            }

            if (string.Equals(src.ItemId, dst.ItemId, StringComparison.Ordinal))
            {
                int maxStack = GetMaxStack(dst.ItemId);
                int room = maxStack - dst.Quantity;
                if (room > 0)
                {
                    int move = Math.Min(room, src.Quantity);
                    SetSlotInternal(toSlot, new ItemStack(dst.ItemId, dst.Quantity + move));
                    int left = src.Quantity - move;
                    SetSlotInternal(fromSlot, left > 0 ? new ItemStack(src.ItemId, left) : ItemStack.Empty);
                    OnChanged?.Invoke();
                    return true;
                }
            }

            // Different item types (or target full): swap
            SetSlotInternal(toSlot, src);
            SetSlotInternal(fromSlot, dst);
            OnChanged?.Invoke();
            return true;
        }

        public bool TrySplitStack(int fromSlot, int toSlot, int splitQuantity)
        {
            if (fromSlot < 0 || fromSlot >= _slots.Length || toSlot < 0 || toSlot >= _slots.Length || fromSlot == toSlot || splitQuantity <= 0)
                return false;

            ItemStack src = _slots[fromSlot];
            if (src.IsEmpty || src.Quantity <= splitQuantity)
                return false;

            ItemStack dst = _slots[toSlot];
            int maxStack = GetMaxStack(src.ItemId);

            if (dst.IsEmpty)
            {
                int move = Math.Min(splitQuantity, maxStack);
                SetSlotInternal(toSlot, new ItemStack(src.ItemId, move));
                SetSlotInternal(fromSlot, new ItemStack(src.ItemId, src.Quantity - move));
                OnChanged?.Invoke();
                return true;
            }

            if (string.Equals(src.ItemId, dst.ItemId, StringComparison.Ordinal))
            {
                int room = maxStack - dst.Quantity;
                if (room <= 0)
                    return false;

                int move = Math.Min(splitQuantity, room);
                SetSlotInternal(toSlot, new ItemStack(dst.ItemId, dst.Quantity + move));
                SetSlotInternal(fromSlot, new ItemStack(src.ItemId, src.Quantity - move));
                OnChanged?.Invoke();
                return true;
            }

            return false;
        }

        public bool TryTransferTo(InventoryContainer target, int fromSlot, int quantity)
        {
            if (target == null || ReferenceEquals(this, target) || fromSlot < 0 || fromSlot >= _slots.Length || quantity <= 0)
                return false;

            ItemStack src = _slots[fromSlot];
            if (src.IsEmpty)
                return false;

            int request = Math.Min(quantity, src.Quantity);
            if (!target.TryAdd(src.ItemId, request, out int added, out _) || added <= 0)
                return false;

            TryRemoveAt(fromSlot, added, out _);
            return true;
        }

        public void SortById()
        {
            var totals = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < _slots.Length; i++)
            {
                ItemStack s = _slots[i];
                if (s.IsEmpty)
                    continue;

                totals.TryGetValue(s.ItemId, out int existing);
                totals[s.ItemId] = existing + s.Quantity;
            }

            var keys = new List<string>(totals.Keys);
            keys.Sort(StringComparer.Ordinal);

            int writeIdx = 0;
            for (int k = 0; k < keys.Count && writeIdx < _slots.Length; k++)
            {
                string id = keys[k];
                int qty = totals[id];
                int maxStack = GetMaxStack(id);
                while (qty > 0 && writeIdx < _slots.Length)
                {
                    int chunk = Math.Min(qty, maxStack);
                    SetSlotInternal(writeIdx++, new ItemStack(id, chunk));
                    qty -= chunk;
                }
            }

            while (writeIdx < _slots.Length)
            {
                SetSlotInternal(writeIdx++, ItemStack.Empty);
            }

            OnChanged?.Invoke();
        }

        public void Clear()
        {
            bool anyCleared = false;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    string id = _slots[i].ItemId;
                    int qty = _slots[i].Quantity;
                    SetSlotInternal(i, ItemStack.Empty);
                    OnItemRemoved?.Invoke(id, qty);
                    anyCleared = true;
                }
            }

            if (anyCleared)
            {
                OnChanged?.Invoke();
            }
        }

        public List<ItemStack> ExportSnapshot()
        {
            var result = new List<ItemStack>(_slots.Length);
            for (int i = 0; i < _slots.Length; i++)
                result.Add(_slots[i]);
            return result;
        }

        public void ImportSnapshot(IReadOnlyList<ItemStack> snapshot)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                ItemStack incoming = snapshot != null && i < snapshot.Count ? snapshot[i] : ItemStack.Empty;
                SetSlotInternal(i, incoming);
            }
            OnChanged?.Invoke();
        }

        internal void RestoreExactSlot(int index, ItemStack stack)
        {
            SetSlotInternal(index, stack);
            OnChanged?.Invoke();
        }

        private int CalculateMaxAddableQuantity(string itemId, int requested)
        {
            int capacity = 0;
            int maxStack = GetMaxStack(itemId);

            if (Config.AllowAutoStack)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    ItemStack s = _slots[i];
                    if (!s.IsEmpty && string.Equals(s.ItemId, itemId, StringComparison.Ordinal))
                        capacity += Math.Max(0, maxStack - s.Quantity);
                }
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsEmpty)
                    capacity += maxStack;
            }

            int result = Math.Min(requested, capacity);

            if (Config.MaxWeight > 0f)
            {
                float unitWeight = GetUnitWeight(itemId);
                if (unitWeight > 0f)
                {
                    float remainingWeight = Math.Max(0f, Config.MaxWeight - CurrentWeight);
                    int weightCap = (int)Math.Floor((remainingWeight + 1e-5f) / unitWeight);
                    result = Math.Min(result, Math.Max(0, weightCap));
                }
            }

            return Math.Max(0, result);
        }

        private void SetSlotInternal(int index, ItemStack next)
        {
            ItemStack prev = _slots[index];
            if (prev.Equals(next))
                return;

            _slots[index] = next;
            OnSlotChanged?.Invoke(new InventorySlotChangedEvent(index, prev, next));
        }
    }
}
