using System;
using System.Collections.Generic;

namespace ContentContent.Save
{
    /// <summary>
    /// Typed key/value state section captured by a single <see cref="ISaveable"/> participant.
    /// </summary>
    public sealed class SaveSection
    {
        private readonly Dictionary<string, SaveValue> _values = new Dictionary<string, SaveValue>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, SaveValue> Values => _values;
        public int Count => _values.Count;

        public void SetBool(string key, bool value) => SetValue(key, SaveValue.FromBool(value));
        public void SetInt(string key, int value) => SetValue(key, SaveValue.FromInt(value));
        public void SetLong(string key, long value) => SetValue(key, SaveValue.FromLong(value));
        public void SetFloat(string key, float value) => SetValue(key, SaveValue.FromFloat(value));
        public void SetDouble(string key, double value) => SetValue(key, SaveValue.FromDouble(value));
        public void SetString(string key, string value) => SetValue(key, SaveValue.FromString(value));

        public void SetValue(string key, SaveValue value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Save key must not be null or empty.", nameof(key));

            if (value.Type == SaveValueType.None)
            {
                _values.Remove(key);
                return;
            }

            _values[key] = value;
        }

        public bool Has(string key) => !string.IsNullOrEmpty(key) && _values.ContainsKey(key);

        public bool TryGet(string key, out SaveValue value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = SaveValue.None;
                return false;
            }

            return _values.TryGetValue(key, out value);
        }

        public bool GetBool(string key, bool defaultValue = false) =>
            TryGet(key, out SaveValue value) ? value.AsBool(defaultValue) : defaultValue;

        public int GetInt(string key, int defaultValue = 0) =>
            TryGet(key, out SaveValue value) ? value.AsInt(defaultValue) : defaultValue;

        public long GetLong(string key, long defaultValue = 0L) =>
            TryGet(key, out SaveValue value) ? value.AsLong(defaultValue) : defaultValue;

        public float GetFloat(string key, float defaultValue = 0f) =>
            TryGet(key, out SaveValue value) ? value.AsFloat(defaultValue) : defaultValue;

        public double GetDouble(string key, double defaultValue = 0.0) =>
            TryGet(key, out SaveValue value) ? value.AsDouble(defaultValue) : defaultValue;

        public string GetString(string key, string defaultValue = "") =>
            TryGet(key, out SaveValue value) ? value.AsString(defaultValue) : (defaultValue ?? string.Empty);

        public bool Remove(string key) => !string.IsNullOrEmpty(key) && _values.Remove(key);

        public void Clear() => _values.Clear();

        public SaveSection Clone()
        {
            var copy = new SaveSection();
            foreach (KeyValuePair<string, SaveValue> kvp in _values)
            {
                copy._values[kvp.Key] = kvp.Value;
            }
            return copy;
        }
    }

    /// <summary>
    /// Summary header stored inside every save file for slot selection menus.
    /// </summary>
    public struct SaveMetadata
    {
        public string SlotId;
        public string DisplayName;
        public int SchemaVersion;
        public long SavedAtUtcTicks;
        public float PlayTimeSeconds;
        public string LocationLabel;
    }

    /// <summary>
    /// Complete versioned save payload containing metadata and named sections.
    /// </summary>
    public sealed class SaveDocument
    {
        private readonly Dictionary<string, SaveSection> _sections = new Dictionary<string, SaveSection>(StringComparer.Ordinal);

        public int SchemaVersion { get; set; }
        public SaveMetadata Metadata { get; set; }
        public IReadOnlyDictionary<string, SaveSection> Sections => _sections;

        public SaveDocument(int schemaVersion = 1)
        {
            SchemaVersion = Math.Max(1, schemaVersion);
            Metadata = new SaveMetadata { SchemaVersion = SchemaVersion };
        }

        public SaveSection GetOrCreateSection(string sectionId)
        {
            if (string.IsNullOrEmpty(sectionId))
                throw new ArgumentException("Section id must not be null or empty.", nameof(sectionId));

            if (!_sections.TryGetValue(sectionId, out SaveSection section))
            {
                section = new SaveSection();
                _sections[sectionId] = section;
            }

            return section;
        }

        public bool TryGetSection(string sectionId, out SaveSection section)
        {
            if (string.IsNullOrEmpty(sectionId))
            {
                section = null;
                return false;
            }

            return _sections.TryGetValue(sectionId, out section);
        }

        public void SetSection(string sectionId, SaveSection section)
        {
            if (string.IsNullOrEmpty(sectionId))
                throw new ArgumentException("Section id must not be null or empty.", nameof(sectionId));
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            _sections[sectionId] = section;
        }

        public bool RemoveSection(string sectionId) => !string.IsNullOrEmpty(sectionId) && _sections.Remove(sectionId);

        public SaveDocument Clone()
        {
            var copy = new SaveDocument(SchemaVersion)
            {
                Metadata = Metadata
            };
            foreach (KeyValuePair<string, SaveSection> kvp in _sections)
            {
                copy._sections[kvp.Key] = kvp.Value.Clone();
            }
            return copy;
        }
    }
}
