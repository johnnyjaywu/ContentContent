using System;

namespace GameSystem.Stats
{
    /// <summary>
    /// Determines how a <see cref="StatModifier"/> is applied during stat calculation.
    /// Enum integer values double as default evaluation order.
    /// </summary>
    public enum ModifierType
    {
        /// <summary>Added directly to the base value (e.g., +10).</summary>
        Flat = 100,

        /// <summary>Summed together and applied as a single percentage multiplier (e.g., +0.20 and +0.30 => * 1.50).</summary>
        PercentAdd = 200,

        /// <summary>Applied multiplicatively one after another (e.g., * (1 + value)).</summary>
        PercentMult = 300
    }

    /// <summary>
    /// Immutable modifier applied to a <see cref="Stat"/>.
    /// </summary>
    public readonly struct StatModifier : IEquatable<StatModifier>
    {
        public float Value { get; }
        public ModifierType Type { get; }
        public int Order { get; }
        public object Source { get; }

        public StatModifier(float value, ModifierType type, int order, object source = null)
        {
            Value = value;
            Type = type;
            Order = order;
            Source = source;
        }

        public StatModifier(float value, ModifierType type, object source = null)
            : this(value, type, (int)type, source)
        {
        }

        public bool Equals(StatModifier other)
        {
            return Value.Equals(other.Value)
                && Type == other.Type
                && Order == other.Order
                && Equals(Source, other.Source);
        }

        public override bool Equals(object obj) => obj is StatModifier other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Value, (int)Type, Order, Source);
    }
}
