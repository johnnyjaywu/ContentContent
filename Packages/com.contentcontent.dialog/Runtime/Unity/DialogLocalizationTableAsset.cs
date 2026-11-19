using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Dialog.Unity
{
    /// <summary>
    /// Simple ScriptableObject string table implementing <see cref="IDialogLocalizer"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogLocalizationTable", menuName = "ContentContent/Dialog/Localization Table")]
    public class DialogLocalizationTableAsset : ScriptableObject, IDialogLocalizer
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            [TextArea(1, 4)]
            public string value;
        }

        [SerializeField] private DialogLocalizationTableAsset _fallbackTable;
        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private Dictionary<string, string> _lookup;

        public string Localize(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            EnsureLookup();
            if (_lookup.TryGetValue(key, out string value))
                return value;

            if (_fallbackTable != null)
                return _fallbackTable.Localize(key);

            return $"<MISSING:{key}>";
        }

        public void RebuildLookup()
        {
            _lookup = new Dictionary<string, string>(_entries.Count, StringComparer.Ordinal);
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (!string.IsNullOrEmpty(e.key))
                    _lookup[e.key] = e.value ?? string.Empty;
            }
        }

        private void EnsureLookup()
        {
            if (_lookup == null)
                RebuildLookup();
        }

        private void OnValidate() => _lookup = null;
    }
}
