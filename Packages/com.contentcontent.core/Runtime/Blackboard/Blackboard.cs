using System;
using System.Collections.Generic;

namespace ContentContent.Core.Blackboards
{
    internal interface IBlackboardSubscribable
    {
        void Unsubscribe(int id);
    }

    /// <summary>
    /// Disposable handle returned by <see cref="Blackboard.Subscribe{T}"/>.
    /// </summary>
    public readonly struct BlackboardSubscription : IDisposable, IEquatable<BlackboardSubscription>
    {
        private readonly IBlackboardSubscribable _entry;
        private readonly int _id;

        public bool IsValid => _entry != null && _id > 0;

        internal BlackboardSubscription(IBlackboardSubscribable entry, int id)
        {
            _entry = entry;
            _id = id;
        }

        public void Dispose()
        {
            if (_entry != null && _id > 0)
            {
                _entry.Unsubscribe(_id);
            }
        }

        public bool Equals(BlackboardSubscription other) => ReferenceEquals(_entry, other._entry) && _id == other._id;
        public override bool Equals(object obj) => obj is BlackboardSubscription other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _entry != null ? _entry.GetHashCode() : 0;
                return (hash * 397) ^ _id;
            }
        }

        public static bool operator ==(BlackboardSubscription left, BlackboardSubscription right) => left.Equals(right);
        public static bool operator !=(BlackboardSubscription left, BlackboardSubscription right) => !left.Equals(right);
    }

    /// <summary>
    /// Zero-boxing typed key-value blackboard supporting optional parent-scope read fallback
    /// and per-key change subscriptions.
    /// </summary>
    public class Blackboard
    {
        private readonly struct SlotKey : IEquatable<SlotKey>
        {
            private readonly string _name;
            private readonly int _nameHash;
            private readonly Type _valueType;

            public string Name => _name;
            public Type ValueType => _valueType;

            public SlotKey(string name, int nameHash, Type valueType)
            {
                _name = name;
                _nameHash = nameHash;
                _valueType = valueType;
            }

            public bool Equals(SlotKey other)
            {
                return _nameHash == other._nameHash
                       && _valueType == other._valueType
                       && string.Equals(_name, other._name, StringComparison.Ordinal);
            }

            public override bool Equals(object obj) => obj is SlotKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_nameHash * 397) ^ _valueType.GetHashCode();
                }
            }
        }

        private interface IEntry
        {
            bool HasValue { get; }
            void ClearValue();
            string FormatValue();
        }

        private sealed class Entry<T> : IEntry, IBlackboardSubscribable
        {
            private sealed class Subscriber
            {
                public int Id;
                public Action<T, T> Callback;
                public bool IsActive;
            }

            public T Value;
            public bool HasValue { get; set; }

            private List<Subscriber> _subscribers;
            private List<Subscriber> _pendingAdds;
            private int _nextId = 1;
            private int _notifyDepth;
            private bool _hasPendingRemovals;

            public BlackboardSubscription Subscribe(Action<T, T> callback)
            {
                int id = _nextId++;
                var sub = new Subscriber { Id = id, Callback = callback, IsActive = true };

                if (_notifyDepth > 0)
                {
                    if (_pendingAdds == null) _pendingAdds = new List<Subscriber>();
                    _pendingAdds.Add(sub);
                }
                else
                {
                    if (_subscribers == null) _subscribers = new List<Subscriber>();
                    _subscribers.Add(sub);
                }

                return new BlackboardSubscription(this, id);
            }

            public void Unsubscribe(int id)
            {
                if (_subscribers != null)
                {
                    for (int i = 0; i < _subscribers.Count; i++)
                    {
                        if (_subscribers[i].Id == id)
                        {
                            if (!_subscribers[i].IsActive)
                                return;

                            _subscribers[i].IsActive = false;
                            if (_notifyDepth > 0)
                            {
                                _hasPendingRemovals = true;
                            }
                            else
                            {
                                _subscribers.RemoveAt(i);
                            }

                            return;
                        }
                    }
                }

                if (_pendingAdds != null)
                {
                    for (int i = 0; i < _pendingAdds.Count; i++)
                    {
                        if (_pendingAdds[i].Id == id)
                        {
                            _pendingAdds[i].IsActive = false;
                            _pendingAdds.RemoveAt(i);
                            return;
                        }
                    }
                }
            }

            public void Notify(T oldValue, T newValue)
            {
                if (_subscribers == null || _subscribers.Count == 0)
                    return;

                _notifyDepth++;
                try
                {
                    int count = _subscribers.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var sub = _subscribers[i];
                        if (sub.IsActive)
                        {
                            sub.Callback(oldValue, newValue);
                        }
                    }
                }
                finally
                {
                    _notifyDepth--;
                    if (_notifyDepth == 0)
                    {
                        FlushPending();
                    }
                }
            }

            public void ClearValue()
            {
                Value = default;
                HasValue = false;
            }

            public string FormatValue()
            {
                return HasValue ? (Value != null ? Value.ToString() : "null") : string.Empty;
            }

            private void FlushPending()
            {
                if (_hasPendingRemovals && _subscribers != null)
                {
                    for (int i = _subscribers.Count - 1; i >= 0; i--)
                    {
                        if (!_subscribers[i].IsActive)
                        {
                            _subscribers.RemoveAt(i);
                        }
                    }

                    _hasPendingRemovals = false;
                }

                if (_pendingAdds != null && _pendingAdds.Count > 0)
                {
                    if (_subscribers == null) _subscribers = new List<Subscriber>();
                    for (int i = 0; i < _pendingAdds.Count; i++)
                    {
                        if (_pendingAdds[i].IsActive)
                        {
                            _subscribers.Add(_pendingAdds[i]);
                        }
                    }

                    _pendingAdds.Clear();
                }
            }
        }

        private readonly Dictionary<SlotKey, IEntry> _entries = new Dictionary<SlotKey, IEntry>();

        /// <summary>Optional parent blackboard consulted for read fallbacks.</summary>
        public Blackboard Parent { get; }

        public Blackboard(Blackboard parent = null)
        {
            Parent = parent;
        }

        /// <summary>
        /// Sets <paramref name="key"/> locally to <paramref name="value"/>.
        /// Notifies local subscribers if the key was newly set or its value changed.
        /// </summary>
        public void Set<T>(BlackboardKey<T> key, T value)
        {
            ValidateKey(key);
            var entry = GetOrCreateEntry(key);

            bool hadValue = entry.HasValue;
            T previous = hadValue ? entry.Value : default;

            if (hadValue && EqualityComparer<T>.Default.Equals(previous, value))
                return;

            entry.Value = value;
            entry.HasValue = true;
            entry.Notify(previous, value);
        }

        /// <summary>
        /// Attempts to read <paramref name="key"/> from this blackboard, falling back to <see cref="Parent"/>.
        /// </summary>
        public bool TryGet<T>(BlackboardKey<T> key, out T value)
        {
            ValidateKey(key);
            var slotKey = CreateSlotKey(key);

            if (_entries.TryGetValue(slotKey, out var rawEntry))
            {
                var entry = (Entry<T>)rawEntry;
                if (entry.HasValue)
                {
                    value = entry.Value;
                    return true;
                }
            }

            if (Parent != null)
            {
                return Parent.TryGet(key, out value);
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns the value for <paramref name="key"/> (checking <see cref="Parent"/> if needed),
        /// or <paramref name="defaultValue"/> if not found.
        /// </summary>
        public T Get<T>(BlackboardKey<T> key, T defaultValue = default)
        {
            return TryGet(key, out T value) ? value : defaultValue;
        }

        /// <summary>True if <paramref name="key"/> has a value set directly on this blackboard.</summary>
        public bool ContainsLocal<T>(BlackboardKey<T> key)
        {
            ValidateKey(key);
            var slotKey = CreateSlotKey(key);
            return _entries.TryGetValue(slotKey, out var rawEntry) && rawEntry.HasValue;
        }

        /// <summary>True if <paramref name="key"/> has a value on this blackboard or any ancestor.</summary>
        public bool Contains<T>(BlackboardKey<T> key)
        {
            return ContainsLocal(key) || (Parent != null && Parent.Contains(key));
        }

        /// <summary>
        /// Removes the local value for <paramref name="key"/> while preserving existing subscriptions.
        /// </summary>
        public bool Remove<T>(BlackboardKey<T> key)
        {
            ValidateKey(key);
            var slotKey = CreateSlotKey(key);

            if (_entries.TryGetValue(slotKey, out var rawEntry) && rawEntry.HasValue)
            {
                rawEntry.ClearValue();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Subscribes to local value changes for <paramref name="key"/>: callback receives (oldValue, newValue).
        /// </summary>
        public BlackboardSubscription Subscribe<T>(BlackboardKey<T> key, Action<T, T> onChanged)
        {
            ValidateKey(key);
            if (onChanged == null)
                throw new ArgumentNullException(nameof(onChanged));

            var entry = GetOrCreateEntry(key);
            return entry.Subscribe(onChanged);
        }

        /// <summary>Clears all local values (preserving subscriptions on existing keys).</summary>
        public void ClearValues()
        {
            foreach (var kvp in _entries)
            {
                kvp.Value.ClearValue();
            }
        }

        /// <summary>Removes all local values and all local subscriptions.</summary>
        public void ClearAll()
        {
            _entries.Clear();
        }

        /// <summary>
        /// Copies human-readable local key/value pairs for diagnostics and Editor telemetry.
        /// </summary>
        public void CopyDebugEntries(List<KeyValuePair<string, string>> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            output.Clear();
            foreach (var kvp in _entries)
            {
                if (!kvp.Value.HasValue)
                    continue;

                string label = $"{kvp.Key.Name} ({kvp.Key.ValueType.Name})";
                output.Add(new KeyValuePair<string, string>(label, kvp.Value.FormatValue()));
            }
        }

        private Entry<T> GetOrCreateEntry<T>(BlackboardKey<T> key)
        {
            var slotKey = CreateSlotKey(key);
            if (!_entries.TryGetValue(slotKey, out var rawEntry))
            {
                var entry = new Entry<T>();
                _entries[slotKey] = entry;
                return entry;
            }

            return (Entry<T>)rawEntry;
        }

        private static SlotKey CreateSlotKey<T>(BlackboardKey<T> key)
        {
            return new SlotKey(key.Name, key.Hash, typeof(T));
        }

        private static void ValidateKey<T>(BlackboardKey<T> key)
        {
            if (string.IsNullOrEmpty(key.Name))
                throw new ArgumentException("BlackboardKey must be initialized with a valid name.", nameof(key));
        }
    }
}
