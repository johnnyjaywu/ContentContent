using System;
using System.Globalization;

namespace ContentContent.Save
{
    public enum SaveValueType
    {
        None = 0,
        Bool = 1,
        Int = 2,
        Long = 3,
        Float = 4,
        Double = 5,
        String = 6
    }

    /// <summary>
    /// Immutable typed primitive union stored in <see cref="SaveSection"/>.
    /// Avoids C# boxing and preserves exact numeric types across JSON serialization.
    /// </summary>
    public readonly struct SaveValue : IEquatable<SaveValue>
    {
        public static SaveValue None => default;

        public SaveValueType Type { get; }
        public bool BoolValue { get; }
        public int IntValue { get; }
        public long LongValue { get; }
        public float FloatValue { get; }
        public double DoubleValue { get; }
        public string StringValue { get; }

        private SaveValue(
            SaveValueType type,
            bool boolValue,
            int intValue,
            long longValue,
            float floatValue,
            double doubleValue,
            string stringValue)
        {
            Type = type;
            BoolValue = boolValue;
            IntValue = intValue;
            LongValue = longValue;
            FloatValue = floatValue;
            DoubleValue = doubleValue;
            StringValue = stringValue ?? string.Empty;
        }

        public static SaveValue FromBool(bool value) =>
            new SaveValue(SaveValueType.Bool, value, value ? 1 : 0, value ? 1L : 0L, value ? 1f : 0f, value ? 1.0 : 0.0, null);

        public static SaveValue FromInt(int value) =>
            new SaveValue(SaveValueType.Int, value != 0, value, value, value, value, null);

        public static SaveValue FromLong(long value) =>
            new SaveValue(SaveValueType.Long, value != 0L, (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, value)), value, value, value, null);

        public static SaveValue FromFloat(float value) =>
            new SaveValue(SaveValueType.Float, Math.Abs(value) > float.Epsilon, (int)value, (long)value, value, value, null);

        public static SaveValue FromDouble(double value) =>
            new SaveValue(SaveValueType.Double, Math.Abs(value) > double.Epsilon, (int)value, (long)value, (float)value, value, null);

        public static SaveValue FromString(string value) =>
            new SaveValue(SaveValueType.String, !string.IsNullOrEmpty(value), 0, 0L, 0f, 0.0, value ?? string.Empty);

        public bool AsBool(bool defaultValue = false) =>
            Type == SaveValueType.None ? defaultValue : BoolValue;

        public int AsInt(int defaultValue = 0)
        {
            switch (Type)
            {
                case SaveValueType.Bool: return BoolValue ? 1 : 0;
                case SaveValueType.Int: return IntValue;
                case SaveValueType.Long: return IntValue;
                case SaveValueType.Float: return (int)FloatValue;
                case SaveValueType.Double: return (int)DoubleValue;
                case SaveValueType.String:
                    return int.TryParse(StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int p) ? p : defaultValue;
                default: return defaultValue;
            }
        }

        public long AsLong(long defaultValue = 0L)
        {
            switch (Type)
            {
                case SaveValueType.Bool: return BoolValue ? 1L : 0L;
                case SaveValueType.Int: return IntValue;
                case SaveValueType.Long: return LongValue;
                case SaveValueType.Float: return (long)FloatValue;
                case SaveValueType.Double: return (long)DoubleValue;
                case SaveValueType.String:
                    return long.TryParse(StringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long p) ? p : defaultValue;
                default: return defaultValue;
            }
        }

        public float AsFloat(float defaultValue = 0f)
        {
            switch (Type)
            {
                case SaveValueType.Bool: return BoolValue ? 1f : 0f;
                case SaveValueType.Int: return IntValue;
                case SaveValueType.Long: return LongValue;
                case SaveValueType.Float: return FloatValue;
                case SaveValueType.Double: return (float)DoubleValue;
                case SaveValueType.String:
                    return float.TryParse(StringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float p) ? p : defaultValue;
                default: return defaultValue;
            }
        }

        public double AsDouble(double defaultValue = 0.0)
        {
            switch (Type)
            {
                case SaveValueType.Bool: return BoolValue ? 1.0 : 0.0;
                case SaveValueType.Int: return IntValue;
                case SaveValueType.Long: return LongValue;
                case SaveValueType.Float: return FloatValue;
                case SaveValueType.Double: return DoubleValue;
                case SaveValueType.String:
                    return double.TryParse(StringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double p) ? p : defaultValue;
                default: return defaultValue;
            }
        }

        public string AsString(string defaultValue = "")
        {
            switch (Type)
            {
                case SaveValueType.Bool: return BoolValue ? "true" : "false";
                case SaveValueType.Int: return IntValue.ToString(CultureInfo.InvariantCulture);
                case SaveValueType.Long: return LongValue.ToString(CultureInfo.InvariantCulture);
                case SaveValueType.Float: return FloatValue.ToString("R", CultureInfo.InvariantCulture);
                case SaveValueType.Double: return DoubleValue.ToString("R", CultureInfo.InvariantCulture);
                case SaveValueType.String: return StringValue;
                default: return defaultValue ?? string.Empty;
            }
        }

        public bool Equals(SaveValue other)
        {
            if (Type != other.Type)
                return false;

            switch (Type)
            {
                case SaveValueType.None: return true;
                case SaveValueType.Bool: return BoolValue == other.BoolValue;
                case SaveValueType.Int: return IntValue == other.IntValue;
                case SaveValueType.Long: return LongValue == other.LongValue;
                case SaveValueType.Float: return FloatValue.Equals(other.FloatValue);
                case SaveValueType.Double: return DoubleValue.Equals(other.DoubleValue);
                case SaveValueType.String: return string.Equals(StringValue, other.StringValue, StringComparison.Ordinal);
                default: return false;
            }
        }

        public override bool Equals(object obj) => obj is SaveValue other && Equals(other);

        public override int GetHashCode()
        {
            switch (Type)
            {
                case SaveValueType.Bool: return HashCode.Combine((int)Type, BoolValue);
                case SaveValueType.Int: return HashCode.Combine((int)Type, IntValue);
                case SaveValueType.Long: return HashCode.Combine((int)Type, LongValue);
                case SaveValueType.Float: return HashCode.Combine((int)Type, FloatValue);
                case SaveValueType.Double: return HashCode.Combine((int)Type, DoubleValue);
                case SaveValueType.String: return HashCode.Combine((int)Type, StringValue);
                default: return 0;
            }
        }
    }
}
