using System;
using System.Collections.Generic;

namespace ContentContent.Stats
{
    /// <summary>
    /// Represents a numeric game attribute with lazy-evaluated <see cref="StatModifier"/> stacking and optional clamping.
    /// </summary>
    public class Stat
    {
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();
        private float _baseValue;
        private float _cachedValue;
        private bool _isDirty = true;

        public float? MinValue { get; }
        public float? MaxValue { get; }

        public IReadOnlyList<StatModifier> Modifiers => _modifiers;

        /// <summary>Invoked when the computed <see cref="Value"/> changes: (oldValue, newValue).</summary>
        public event Action<float, float> OnValueChanged;

        public float BaseValue
        {
            get => _baseValue;
            set
            {
                if (_baseValue.Equals(value))
                    return;

                float oldValue = Value;
                _baseValue = value;
                MarkDirtyAndNotify(oldValue);
            }
        }

        public float Value
        {
            get
            {
                if (_isDirty)
                {
                    _cachedValue = CalculateFinalValue();
                    _isDirty = false;
                }
                return _cachedValue;
            }
        }

        public Stat(float baseValue, float? minValue = null, float? maxValue = null)
        {
            if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
                throw new ArgumentException("minValue cannot be greater than maxValue.");

            _baseValue = baseValue;
            MinValue = minValue;
            MaxValue = maxValue;
            _cachedValue = CalculateFinalValue();
            _isDirty = false;
        }

        public void AddModifier(StatModifier modifier)
        {
            float oldValue = Value;

            int index = 0;
            while (index < _modifiers.Count && _modifiers[index].Order <= modifier.Order)
            {
                index++;
            }
            _modifiers.Insert(index, modifier);

            MarkDirtyAndNotify(oldValue);
        }

        public bool RemoveModifier(StatModifier modifier)
        {
            int index = _modifiers.IndexOf(modifier);
            if (index < 0)
                return false;

            float oldValue = Value;
            _modifiers.RemoveAt(index);
            MarkDirtyAndNotify(oldValue);
            return true;
        }

        public int RemoveAllModifiersFromSource(object source)
        {
            if (source == null)
                return 0;

            float oldValue = Value;
            int removed = 0;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (Equals(_modifiers[i].Source, source))
                {
                    _modifiers.RemoveAt(i);
                    removed++;
                }
            }

            if (removed > 0)
            {
                MarkDirtyAndNotify(oldValue);
            }

            return removed;
        }

        public void ClearModifiers()
        {
            if (_modifiers.Count == 0)
                return;

            float oldValue = Value;
            _modifiers.Clear();
            MarkDirtyAndNotify(oldValue);
        }

        private void MarkDirtyAndNotify(float oldValue)
        {
            _isDirty = true;
            if (OnValueChanged != null)
            {
                float newValue = Value;
                if (!oldValue.Equals(newValue))
                {
                    OnValueChanged.Invoke(oldValue, newValue);
                }
            }
        }

        private float CalculateFinalValue()
        {
            float finalValue = _baseValue;
            float percentAddSum = 0f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                StatModifier mod = _modifiers[i];
                switch (mod.Type)
                {
                    case ModifierType.Flat:
                        finalValue += mod.Value;
                        break;

                    case ModifierType.PercentAdd:
                        percentAddSum += mod.Value;
                        bool isLastPercentAdd = i + 1 >= _modifiers.Count
                            || _modifiers[i + 1].Type != ModifierType.PercentAdd;
                        if (isLastPercentAdd)
                        {
                            finalValue *= 1f + percentAddSum;
                            percentAddSum = 0f;
                        }
                        break;

                    case ModifierType.PercentMult:
                        finalValue *= 1f + mod.Value;
                        break;
                }
            }

            if (MinValue.HasValue && finalValue < MinValue.Value)
                finalValue = MinValue.Value;
            if (MaxValue.HasValue && finalValue > MaxValue.Value)
                finalValue = MaxValue.Value;

            return (float)Math.Round(finalValue, 4);
        }
    }
}
