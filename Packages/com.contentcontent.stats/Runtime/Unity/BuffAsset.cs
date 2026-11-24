using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Stats.Unity
{
    /// <summary>
    /// Reusable <see cref="ScriptableObject"/> defining a timed or permanent status effect / buff
    /// that modifies stats on a <see cref="StatsComponent"/> without writing C#.
    /// </summary>
    [CreateAssetMenu(fileName = "Buff", menuName = "ContentContent/Stats/Buff", order = 1)]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/stats/quickstart")]
    public class BuffAsset : ScriptableObject
    {
        [Serializable]
        public struct ModifierEntry
        {
            [Tooltip("Stat identifier matching an entry in StatSheetAsset (e.g., Attack, MaxHealth, MoveSpeed).")]
            public string statId;

            [Tooltip("Numeric modifier magnitude (e.g., 10 for Flat, or 0.25 for +25% PercentAdd/Mult).")]
            public float value;

            [Tooltip("Flat adds directly; PercentAdd sums percentages before multiplying; PercentMult compounds.")]
            public ModifierType type;
        }

        [Tooltip("Unique buff ID used for stacking and removal.")]
        [SerializeField] private string _buffId = "Buff";

        [Tooltip("Duration in seconds (0 = permanent until removed manually).")]
        [SerializeField] private float _duration = 5f;

        [Tooltip("Behavior when re-applying this buff while already active.")]
        [SerializeField] private BuffStackingPolicy _stackingPolicy = BuffStackingPolicy.RefreshDuration;

        [Tooltip("Maximum simultaneous stacks when Stacking Policy is StackValue.")]
        [SerializeField] private int _maxStacks = 1;

        [Tooltip("Stat modifiers granted per stack while this buff is active.")]
        [SerializeField] private List<ModifierEntry> _modifiers = new List<ModifierEntry>();

        public string BuffId => string.IsNullOrWhiteSpace(_buffId) ? name : _buffId.Trim();
        public float Duration => _duration;
        public BuffStackingPolicy StackingPolicy => _stackingPolicy;
        public int MaxStacks => _maxStacks;
        public IReadOnlyList<ModifierEntry> Modifiers => _modifiers;

        private void OnValidate()
        {
            _duration = Mathf.Max(0f, _duration);
            _maxStacks = Mathf.Max(1, _maxStacks);
            if (string.IsNullOrWhiteSpace(_buffId))
            {
                _buffId = name;
            }

            if (_modifiers != null)
            {
                for (int i = 0; i < _modifiers.Count; i++)
                {
                    ModifierEntry m = _modifiers[i];
                    if (!Enum.IsDefined(typeof(ModifierType), m.type))
                    {
                        m.type = ModifierType.Flat;
                        _modifiers[i] = m;
                    }
                }
            }
        }

        /// <summary>
        /// Builds a pure-C# <see cref="BuffDefinition{TStatId}"/> from this asset.
        /// </summary>
        public BuffDefinition<string> BuildDefinition()
        {
            var entries = new List<BuffModifierEntry<string>>(_modifiers.Count);
            for (int i = 0; i < _modifiers.Count; i++)
            {
                ModifierEntry m = _modifiers[i];
                if (string.IsNullOrWhiteSpace(m.statId))
                    continue;

                ModifierType type = Enum.IsDefined(typeof(ModifierType), m.type) ? m.type : ModifierType.Flat;
                entries.Add(new BuffModifierEntry<string>(m.statId.Trim(), m.value, type));
            }

            return new BuffDefinition<string>(
                id: BuffId,
                duration: Mathf.Max(0f, _duration),
                modifiers: entries,
                stackingPolicy: _stackingPolicy,
                maxStacks: Mathf.Max(1, _maxStacks));
        }
    }
}
