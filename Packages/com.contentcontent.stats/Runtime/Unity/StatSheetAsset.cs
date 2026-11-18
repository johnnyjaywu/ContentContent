using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Stats.Unity
{
    /// <summary>
    /// Designer-authored ScriptableObject defining base stats and optional min/max clamps.
    /// </summary>
    [CreateAssetMenu(fileName = "StatSheet", menuName = "ContentContent/Stats/Stat Sheet")]
    public class StatSheetAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string statId;
            public float baseValue;
            public bool clampMin;
            public float minValue;
            public bool clampMax;
            public float maxValue;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        public void Populate(StatCollection<string> collection)
        {
            if (collection == null)
                return;

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (string.IsNullOrEmpty(e.statId))
                    continue;

                collection.RegisterStat(
                    e.statId,
                    e.baseValue,
                    e.clampMin ? e.minValue : (float?)null,
                    e.clampMax ? e.maxValue : (float?)null);
            }
        }
    }
}
