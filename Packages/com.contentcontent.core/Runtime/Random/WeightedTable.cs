using System;
using System.Collections.Generic;

namespace ContentContent.Core.Random
{
    /// <summary>
    /// Mutable weighted random selection table backed by lazy-rebuilt prefix sums and O(log n) binary search.
    /// </summary>
    public class WeightedTable<T>
    {
        public readonly struct Entry
        {
            public readonly T Item;
            public readonly float Weight;

            public Entry(T item, float weight)
            {
                Item = item;
                Weight = weight;
            }
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<float> _prefixSums = new List<float>();
        private float _totalWeight;
        private bool _dirty;

        /// <summary>Number of entries in the table.</summary>
        public int Count => _entries.Count;

        /// <summary>Sum of all entry weights.</summary>
        public float TotalWeight
        {
            get
            {
                EnsureBuilt();
                return _totalWeight;
            }
        }

        public Entry this[int index] => _entries[index];

        /// <summary>Adds an item with a strictly positive <paramref name="weight"/>.</summary>
        public void Add(T item, float weight)
        {
            if (float.IsNaN(weight) || float.IsInfinity(weight) || weight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be a finite number greater than 0.");

            _entries.Add(new Entry(item, weight));
            _dirty = true;
        }

        /// <summary>Removes the entry at <paramref name="index"/>.</summary>
        public void RemoveAt(int index)
        {
            _entries.RemoveAt(index);
            _dirty = true;
        }

        /// <summary>Removes all entries from the table.</summary>
        public void Clear()
        {
            _entries.Clear();
            _prefixSums.Clear();
            _totalWeight = 0f;
            _dirty = false;
        }

        /// <summary>
        /// Picks an item weighted by its relative weight using <paramref name="random"/>.
        /// </summary>
        public T Pick(IRandom random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            if (_entries.Count == 0)
                throw new InvalidOperationException("Cannot pick from an empty WeightedTable.");

            EnsureBuilt();

            float target = random.NextFloat() * _totalWeight;
            int low = 0;
            int high = _prefixSums.Count - 1;

            while (low < high)
            {
                int mid = low + ((high - low) >> 1);
                if (target < _prefixSums[mid])
                {
                    high = mid;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return _entries[low].Item;
        }

        private void EnsureBuilt()
        {
            if (!_dirty)
                return;

            _prefixSums.Clear();
            float sum = 0f;
            for (int i = 0; i < _entries.Count; i++)
            {
                sum += _entries[i].Weight;
                _prefixSums.Add(sum);
            }

            _totalWeight = sum;
            _dirty = false;
        }
    }
}
