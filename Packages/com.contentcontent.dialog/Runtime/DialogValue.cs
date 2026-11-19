using System;
using System.Globalization;

namespace ContentContent.Dialog
{
    public enum DialogValueType
    {
        Bool = 0,
        Int = 1,
        Float = 2,
        String = 3
    }

    /// <summary>
    /// Immutable typed value stored in a <see cref="DialogVariableStore"/>.
    /// </summary>
    public readonly struct DialogValue : IEquatable<DialogValue>
    {
        public DialogValueType Type { get; }
        public bool BoolValue { get; }
        public int IntValue { get; }
        public float FloatValue { get; }
        public string StringValue { get; }

        private DialogValue(DialogValueType type, bool boolValue, int intValue, float floatValue, string stringValue)
        {
            Type = type;
            BoolValue = boolValue;
            IntValue = intValue;
            FloatValue = floatValue;
            StringValue = stringValue ?? string.Empty;
        }

        public static DialogValue FromBool(bool value) => new DialogValue(DialogValueType.Bool, value, value ? 1 : 0, value ? 1f : 0f, string.Empty);
        public static DialogValue FromInt(int value) => new DialogValue(DialogValueType.Int, value != 0, value, value, string.Empty);
        public static DialogValue FromFloat(float value) => new DialogValue(DialogValueType.Float, Math.Abs(value) > float.Epsilon, (int)value, value, string.Empty);
        public static DialogValue FromString(string value) => new DialogValue(DialogValueType.String, !string.IsNullOrEmpty(value), 0, 0f, value ?? string.Empty);

        public bool AsBool()
        {
            switch (Type)
            {
                case DialogValueType.Bool: return BoolValue;
                case DialogValueType.Int: return IntValue != 0;
                case DialogValueType.Float: return Math.Abs(FloatValue) > float.Epsilon;
                case DialogValueType.String: return !string.IsNullOrEmpty(StringValue);
                default: return false;
            }
        }

        public int AsInt()
        {
            switch (Type)
            {
                case DialogValueType.Bool: return BoolValue ? 1 : 0;
                case DialogValueType.Int: return IntValue;
                case DialogValueType.Float: return (int)FloatValue;
                case DialogValueType.String:
                    return int.TryParse(StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 0;
                default: return 0;
            }
        }

        public float AsFloat()
        {
            switch (Type)
            {
                case DialogValueType.Bool: return BoolValue ? 1f : 0f;
                case DialogValueType.Int: return IntValue;
                case DialogValueType.Float: return FloatValue;
                case DialogValueType.String:
                    return float.TryParse(StringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : 0f;
                default: return 0f;
            }
        }

        public string ToDisplayString()
        {
            switch (Type)
            {
                case DialogValueType.Bool: return BoolValue ? "true" : "false";
                case DialogValueType.Int: return IntValue.ToString(CultureInfo.InvariantCulture);
                case DialogValueType.Float: return FloatValue.ToString("0.##", CultureInfo.InvariantCulture);
                case DialogValueType.String: return StringValue;
                default: return string.Empty;
            }
        }

        public bool Equals(DialogValue other)
        {
            if (IsNumeric(Type) && IsNumeric(other.Type))
                return Math.Abs(AsFloat() - other.AsFloat()) <= 0.0001f;

            if (Type != other.Type)
                return false;

            switch (Type)
            {
                case DialogValueType.Bool: return BoolValue == other.BoolValue;
                case DialogValueType.String: return string.Equals(StringValue, other.StringValue, StringComparison.Ordinal);
                default: return false;
            }
        }

        public int CompareNumeric(DialogValue other)
        {
            float a = AsFloat();
            float b = other.AsFloat();
            if (Math.Abs(a - b) <= 0.0001f)
                return 0;
            return a < b ? -1 : 1;
        }

        public override bool Equals(object obj) => obj is DialogValue other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Type * 397;
                switch (Type)
                {
                    case DialogValueType.Bool: return hash ^ BoolValue.GetHashCode();
                    case DialogValueType.Int: return hash ^ IntValue.GetHashCode();
                    case DialogValueType.Float: return hash ^ FloatValue.GetHashCode();
                    case DialogValueType.String: return hash ^ StringValue.GetHashCode();
                    default: return hash;
                }
            }
        }

        public override string ToString() => ToDisplayString();

        private static bool IsNumeric(DialogValueType type) => type == DialogValueType.Int || type == DialogValueType.Float;
    }
}
