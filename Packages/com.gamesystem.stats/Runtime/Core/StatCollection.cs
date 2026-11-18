using System;
using System.Collections.Generic;

namespace GameSystem.Stats
{
    /// <summary>
    /// Keyed collection of <see cref="Stat"/> instances (keyed by enum, string, or any identifier type).
    /// </summary>
    public class StatCollection<TStatId>
    {
        private readonly Dictionary<TStatId, Stat> _stats = new Dictionary<TStatId, Stat>();

        public IReadOnlyDictionary<TStatId, Stat> Stats => _stats;

        public Stat RegisterStat(TStatId id, float baseValue, float? minValue = null, float? maxValue = null)
        {
            var stat = new Stat(baseValue, minValue, maxValue);
            _stats[id] = stat;
            return stat;
        }

        public bool TryGetStat(TStatId id, out Stat stat) => _stats.TryGetValue(id, out stat);

        public Stat GetStat(TStatId id)
        {
            if (!_stats.TryGetValue(id, out var stat))
                throw new KeyNotFoundException($"Stat '{id}' is not registered in this collection.");
            return stat;
        }

        public float GetValue(TStatId id, float fallback = 0f)
        {
            return _stats.TryGetValue(id, out var stat) ? stat.Value : fallback;
        }

        public bool AddModifier(TStatId id, StatModifier modifier)
        {
            if (!_stats.TryGetValue(id, out var stat))
                return false;

            stat.AddModifier(modifier);
            return true;
        }

        public int RemoveAllModifiersFromSource(object source)
        {
            int totalRemoved = 0;
            foreach (var pair in _stats)
            {
                totalRemoved += pair.Value.RemoveAllModifiersFromSource(source);
            }
            return totalRemoved;
        }
    }
}
