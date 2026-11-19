using System;
using System.Collections.Generic;

namespace ContentContent.Dialog
{
    public interface IDialogVariableStore
    {
        event Action<string, DialogValue> OnVariableChanged;

        bool Has(string name);
        bool TryGet(string name, out DialogValue value);
        void Set(string name, DialogValue value);
        bool GetBool(string name, bool defaultValue = false);
        int GetInt(string name, int defaultValue = 0);
        float GetFloat(string name, float defaultValue = 0f);
        string GetString(string name, string defaultValue = "");
        IReadOnlyDictionary<string, DialogValue> Snapshot();
        void Restore(IReadOnlyDictionary<string, DialogValue> values);
        void Clear();
    }

    /// <summary>
    /// Flat dictionary-backed variable store shared across dialog runs, gameplay, and saves.
    /// </summary>
    public sealed class DialogVariableStore : IDialogVariableStore
    {
        private readonly Dictionary<string, DialogValue> _values = new Dictionary<string, DialogValue>(StringComparer.Ordinal);

        public event Action<string, DialogValue> OnVariableChanged;

        public bool Has(string name) => !string.IsNullOrEmpty(name) && _values.ContainsKey(name);

        public bool TryGet(string name, out DialogValue value)
        {
            if (string.IsNullOrEmpty(name))
            {
                value = default;
                return false;
            }

            return _values.TryGetValue(name, out value);
        }

        public void Set(string name, DialogValue value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Variable name cannot be null or empty.", nameof(name));

            if (_values.TryGetValue(name, out DialogValue existing) && existing.Equals(value))
                return;

            _values[name] = value;
            OnVariableChanged?.Invoke(name, value);
        }

        public void SetBool(string name, bool value) => Set(name, DialogValue.FromBool(value));
        public void SetInt(string name, int value) => Set(name, DialogValue.FromInt(value));
        public void SetFloat(string name, float value) => Set(name, DialogValue.FromFloat(value));
        public void SetString(string name, string value) => Set(name, DialogValue.FromString(value));

        public bool GetBool(string name, bool defaultValue = false) =>
            TryGet(name, out DialogValue value) ? value.AsBool() : defaultValue;

        public int GetInt(string name, int defaultValue = 0) =>
            TryGet(name, out DialogValue value) ? value.AsInt() : defaultValue;

        public float GetFloat(string name, float defaultValue = 0f) =>
            TryGet(name, out DialogValue value) ? value.AsFloat() : defaultValue;

        public string GetString(string name, string defaultValue = "") =>
            TryGet(name, out DialogValue value) ? value.ToDisplayString() : defaultValue;

        public IReadOnlyDictionary<string, DialogValue> Snapshot() =>
            new Dictionary<string, DialogValue>(_values, StringComparer.Ordinal);

        public void Restore(IReadOnlyDictionary<string, DialogValue> values)
        {
            _values.Clear();
            if (values == null)
                return;

            foreach (KeyValuePair<string, DialogValue> pair in values)
            {
                if (string.IsNullOrEmpty(pair.Key))
                    continue;

                _values[pair.Key] = pair.Value;
                OnVariableChanged?.Invoke(pair.Key, pair.Value);
            }
        }

        public void Clear() => _values.Clear();
    }
}
