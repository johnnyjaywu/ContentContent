using System;
using System.Collections.Generic;

namespace ContentContent.Dialog
{
    public interface IDialogLocalizer
    {
        string Localize(string key);
    }

    /// <summary>
    /// Returns the input key unchanged (useful for prototyping or raw-text graphs).
    /// </summary>
    public sealed class PassthroughLocalizer : IDialogLocalizer
    {
        public static readonly PassthroughLocalizer Instance = new PassthroughLocalizer();

        public string Localize(string key) => key ?? string.Empty;
    }

    /// <summary>
    /// Dictionary-backed localizer with optional fallback table and visible missing-key placeholders.
    /// </summary>
    public sealed class DictionaryLocalizer : IDialogLocalizer
    {
        private readonly IReadOnlyDictionary<string, string> _primary;
        private readonly IReadOnlyDictionary<string, string> _fallback;

        public DictionaryLocalizer(
            IReadOnlyDictionary<string, string> primary,
            IReadOnlyDictionary<string, string> fallback = null)
        {
            _primary = primary ?? new Dictionary<string, string>(StringComparer.Ordinal);
            _fallback = fallback;
        }

        public string Localize(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            if (_primary.TryGetValue(key, out string localized))
                return localized;

            if (_fallback != null && _fallback.TryGetValue(key, out string fallbackValue))
                return fallbackValue;

            return $"<MISSING:{key}>";
        }
    }
}
