using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContentContent.Core.Blackboards.Unity
{
    /// <summary>
    /// Code-free MonoBehaviour adapter exposing a <see cref="Blackboard"/> instance with optional
    /// parent <see cref="BlackboardBehaviour"/> fallback and Inspector-seeded initial keys.
    /// </summary>
    [AddComponentMenu("ContentContent/Core/Blackboard")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/core/quickstart")]
    public class BlackboardBehaviour : MonoBehaviour
    {
        public enum InitialValueType
        {
            Bool = 0,
            Int = 1,
            Float = 2,
            String = 3
        }

        [Serializable]
        public struct InitialEntry
        {
            public string key;
            public InitialValueType type;
            public bool boolValue;
            public int intValue;
            public float floatValue;
            public string stringValue;
        }

        [Tooltip("Optional parent Blackboard consulted when a key is not set locally (e.g., Squad or World scope).")]
        [SerializeField] private BlackboardBehaviour _parentBlackboard;

        [Tooltip("Initial values seeded automatically when this Blackboard is initialized.")]
        [SerializeField] private List<InitialEntry> _initialEntries = new List<InitialEntry>();

        private Blackboard _blackboard;

        /// <summary>Optional parent <see cref="BlackboardBehaviour"/>.</summary>
        public BlackboardBehaviour ParentBlackboard
        {
            get => _parentBlackboard;
            set
            {
                if (value == this)
                {
                    _parentBlackboard = null;
                    return;
                }

                _parentBlackboard = value;
            }
        }

        /// <summary>The underlying <see cref="Blackboard"/> instance (lazily initialized).</summary>
        public Blackboard Blackboard
        {
            get
            {
                if (_blackboard == null)
                {
                    _blackboard = _parentBlackboard != null && _parentBlackboard != this
                        ? new Blackboard(_parentBlackboard.Blackboard)
                        : new Blackboard();
                    SeedInitialEntries();
                }

                return _blackboard;
            }
        }

        private void OnValidate()
        {
            if (_parentBlackboard == this)
            {
                _parentBlackboard = null;
            }
        }

        private void OnDestroy()
        {
            _blackboard?.ClearAll();
        }

        private void SeedInitialEntries()
        {
            if (_initialEntries == null)
            {
                return;
            }

            for (int i = 0; i < _initialEntries.Count; i++)
            {
                InitialEntry entry = _initialEntries[i];
                if (string.IsNullOrWhiteSpace(entry.key))
                {
                    continue;
                }

                string keyName = entry.key.Trim();
                switch (entry.type)
                {
                    case InitialValueType.Bool:
                        _blackboard.Set(BlackboardKey.Create<bool>(keyName), entry.boolValue);
                        break;
                    case InitialValueType.Int:
                        _blackboard.Set(BlackboardKey.Create<int>(keyName), entry.intValue);
                        break;
                    case InitialValueType.Float:
                        _blackboard.Set(BlackboardKey.Create<float>(keyName), entry.floatValue);
                        break;
                    case InitialValueType.String:
                        _blackboard.Set(BlackboardKey.Create<string>(keyName), entry.stringValue ?? string.Empty);
                        break;
                }
            }
        }
    }
}
