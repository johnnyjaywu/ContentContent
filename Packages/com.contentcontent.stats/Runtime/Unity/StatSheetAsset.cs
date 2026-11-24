using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Stats.Unity
{
    /// <summary>
    /// Designer-authored <see cref="ScriptableObject"/> defining base stats and optional min/max clamps.
    /// </summary>
    [CreateAssetMenu(fileName = "StatSheet", menuName = "ContentContent/Stats/Stat Sheet", order = 0)]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/stats/quickstart")]
    public class StatSheetAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("Unique name of this stat (e.g., MaxHealth, Attack, Defense, MoveSpeed).")]
            public string statId;

            [Tooltip("Starting base value before equipment or buffs are applied.")]
            public float baseValue;

            [Tooltip("Enforce a lower clamp on the final calculated stat value.")]
            public bool clampMin;

            public float minValue;

            [Tooltip("Enforce an upper clamp on the final calculated stat value.")]
            public bool clampMax;

            public float maxValue;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        private void OnValidate()
        {
            if (_entries == null)
                return;

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (e.clampMin && e.clampMax && e.minValue > e.maxValue)
                {
                    e.maxValue = e.minValue;
                    _entries[i] = e;
                }
            }
        }

        /// <summary>
        /// Replaces entries with a balanced starter RPG sheet (MaxHealth, Attack, Defense, MoveSpeed).
        /// </summary>
        public void ApplyStarterRpgPreset()
        {
            if (_entries == null)
            {
                _entries = new List<Entry>();
            }

            _entries.Clear();
            _entries.Add(new Entry { statId = "MaxHealth", baseValue = 100f, clampMin = true, minValue = 1f });
            _entries.Add(new Entry { statId = "Attack", baseValue = 20f, clampMin = true, minValue = 0f });
            _entries.Add(new Entry { statId = "Defense", baseValue = 5f, clampMin = true, minValue = 0f });
            _entries.Add(new Entry { statId = "MoveSpeed", baseValue = 6f, clampMin = true, minValue = 0f });
        }

        public void Populate(StatCollection<string> collection)
        {
            if (collection == null || _entries == null)
                return;

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (string.IsNullOrWhiteSpace(e.statId))
                    continue;

                float? min = e.clampMin ? e.minValue : (float?)null;
                float? max = e.clampMax ? (min.HasValue && e.maxValue < min.Value ? min.Value : e.maxValue) : (float?)null;

                collection.RegisterStat(e.statId.Trim(), e.baseValue, min, max);
            }
        }
    }
}
