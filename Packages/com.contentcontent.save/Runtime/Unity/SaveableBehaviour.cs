using System;
using UnityEngine;

namespace ContentContent.Save.Unity
{
    /// <summary>
    /// Base <see cref="MonoBehaviour"/> implementing <see cref="ISaveable"/> and auto-registering
    /// with <see cref="SaveManagerBehaviour"/>. Stays registered while deactivated so disabled entities
    /// still save and restore state cleanly.
    /// </summary>
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/save/quickstart")]
    public abstract class SaveableBehaviour : MonoBehaviour, ISaveable
    {
        [Tooltip("Optional SaveManagerBehaviour. Auto-discovered in the scene if left empty.")]
        [SerializeField] private SaveManagerBehaviour _saveManager;

        [Tooltip("Stable unique save section identifier across play sessions.")]
        [SerializeField] private string _saveId = string.Empty;

        private SaveManagerBehaviour _registeredManager;

        public string SaveId => _saveId;

        protected virtual void Reset()
        {
            if (_saveManager == null)
                _saveManager = FindAnyObjectByType<SaveManagerBehaviour>();

            if (string.IsNullOrWhiteSpace(_saveId))
                GenerateUniqueId();
        }

        protected virtual void Awake()
        {
            RegisterWithManager();
        }

        protected virtual void OnEnable()
        {
            RegisterWithManager();
        }

        protected virtual void OnDestroy()
        {
            UnregisterFromManager();
        }

        [ContextMenu("Generate Unique Save ID")]
        public void GenerateUniqueId()
        {
            _saveId = Guid.NewGuid().ToString("N");
        }

        /// <summary>Explicitly registers this participant with the scene <see cref="SaveManagerBehaviour"/>.</summary>
        public void RegisterWithManager()
        {
            if (_saveManager == null)
                _saveManager = FindAnyObjectByType<SaveManagerBehaviour>();

            if (_saveManager == null || string.IsNullOrWhiteSpace(_saveId))
                return;

            if (_registeredManager == _saveManager)
                return;

            UnregisterFromManager();
            _saveManager.Manager.Register(this);
            _registeredManager = _saveManager;
        }

        public void UnregisterFromManager()
        {
            if (_registeredManager != null)
            {
                _registeredManager.Manager.Unregister(this);
                _registeredManager = null;
            }
        }

        public abstract void CaptureState(SaveSection section);
        public abstract void RestoreState(SaveSection section);
    }
}
