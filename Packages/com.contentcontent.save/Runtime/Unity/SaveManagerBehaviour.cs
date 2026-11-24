using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace ContentContent.Save.Unity
{
    /// <summary>
    /// Code-free scene manager hosting a <see cref="SaveManager"/> with disk or in-memory storage,
    /// automatic play-time tracking, scheduled autosave, and Inspector UnityEvents.
    /// </summary>
    [AddComponentMenu("ContentContent/Save/Save Manager")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/save/quickstart")]
    public class SaveManagerBehaviour : MonoBehaviour
    {
        [Header("Schema & Storage")]
        [Tooltip("Current save schema version. Increment alongside ISaveMigration implementations when changing save keys.")]
        [SerializeField, Min(1)] private int _schemaVersion = 1;

        [Tooltip("Subfolder under Application.persistentDataPath where .json save files are written.")]
        [SerializeField] private string _saveSubdirectory = "Saves";

        [Tooltip("Default slot identifier used by SaveDefaultSlot() and LoadDefaultSlot().")]
        [SerializeField] private string _defaultSlotId = "slot_0";

        [Tooltip("Slot identifier used by SaveAutosave() and timed autosaves.")]
        [SerializeField] private string _autosaveSlotId = "autosave";

        [Tooltip("Stores saves in RAM only (useful for prototypes and automated tests).")]
        [SerializeField] private bool _useInMemoryStorage;

        [Header("Lifecycle")]
        [Tooltip("Automatically loads Default Slot Id on Start() if that slot exists.")]
        [SerializeField] private bool _autoLoadDefaultOnStart;

        [Tooltip("Optional interval (seconds) between automatic saves to Autosave Slot Id (0 disables timer).")]
        [SerializeField, Min(0f)] private float _autoSaveIntervalSeconds;

        [Header("Events")]
        [SerializeField] private UnityEvent<string> _onSlotSaved = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _onSlotLoaded = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _onSlotDeleted = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _onLoadFailed = new UnityEvent<string>();

        private SaveManager _manager;
        private float _playTimeSeconds;
        private float _autoSaveTimer;

        public SaveManager Manager
        {
            get
            {
                EnsureInitialized();
                return _manager;
            }
        }

        public int SchemaVersion => Mathf.Max(1, _schemaVersion);
        public bool UseInMemoryStorage => _useInMemoryStorage;
        public string DefaultSlotId => string.IsNullOrWhiteSpace(_defaultSlotId) ? "slot_0" : _defaultSlotId.Trim();
        public string AutosaveSlotId => string.IsNullOrWhiteSpace(_autosaveSlotId) ? "autosave" : _autosaveSlotId.Trim();

        public string ResolvedSaveDirectory
        {
            get
            {
                string sub = string.IsNullOrWhiteSpace(_saveSubdirectory) ? "Saves" : _saveSubdirectory.Trim();
                return Path.Combine(Application.persistentDataPath, sub);
            }
        }

        public UnityEvent<string> OnSlotSaved => _onSlotSaved ?? (_onSlotSaved = new UnityEvent<string>());
        public UnityEvent<string> OnSlotLoaded => _onSlotLoaded ?? (_onSlotLoaded = new UnityEvent<string>());
        public UnityEvent<string> OnSlotDeleted => _onSlotDeleted ?? (_onSlotDeleted = new UnityEvent<string>());
        public UnityEvent<string> OnLoadFailed => _onLoadFailed ?? (_onLoadFailed = new UnityEvent<string>());

        private void OnValidate()
        {
            _schemaVersion = Mathf.Max(1, _schemaVersion);
            _autoSaveIntervalSeconds = Mathf.Max(0f, _autoSaveIntervalSeconds);
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            if (_autoLoadDefaultOnStart && SlotExists(DefaultSlotId))
            {
                LoadDefaultSlot();
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
                return;

            _playTimeSeconds += dt;

            if (_autoSaveIntervalSeconds > 0f)
            {
                _autoSaveTimer += dt;
                if (_autoSaveTimer >= _autoSaveIntervalSeconds)
                {
                    _autoSaveTimer = 0f;
                    SaveAutosave();
                }
            }
        }

        private void OnDestroy()
        {
            if (_manager == null)
                return;

            _manager.OnAfterSave -= HandleAfterSave;
            _manager.OnAfterLoad -= HandleAfterLoad;
            _manager.OnSaveDeleted -= HandleDeleted;
        }

        public void SaveDefaultSlot() => SaveSlot(DefaultSlotId);

        public bool LoadDefaultSlot() => LoadSlot(DefaultSlotId);

        public void SaveSlot(string slotId)
        {
            Manager.SaveSlot(slotId, displayName: slotId, playTimeSeconds: _playTimeSeconds);
        }

        public bool LoadSlot(string slotId)
        {
            if (Manager.TryLoadSlot(slotId, out SaveDocument doc))
            {
                _playTimeSeconds = Math.Max(0f, doc.Metadata.PlayTimeSeconds);
                return true;
            }

            _onLoadFailed?.Invoke(slotId ?? string.Empty);
            return false;
        }

        public void SaveAutosave()
        {
            Manager.SaveAutosave(AutosaveSlotId, displayName: "Autosave", playTimeSeconds: _playTimeSeconds);
        }

        public bool DeleteSlot(string slotId) => Manager.DeleteSlot(slotId);

        public bool SlotExists(string slotId) => Manager.SlotExists(slotId);

        private void EnsureInitialized()
        {
            if (_manager != null)
                return;

            ISaveStorage storage = _useInMemoryStorage
                ? (ISaveStorage)new InMemorySaveStorage()
                : new FileSaveStorage(ResolvedSaveDirectory);

            _manager = new SaveManager(storage, SchemaVersion);
            _manager.OnAfterSave += HandleAfterSave;
            _manager.OnAfterLoad += HandleAfterLoad;
            _manager.OnSaveDeleted += HandleDeleted;
        }

        private void HandleAfterSave(SaveDocument doc) => _onSlotSaved?.Invoke(doc.Metadata.SlotId);
        private void HandleAfterLoad(SaveDocument doc) => _onSlotLoaded?.Invoke(doc.Metadata.SlotId);
        private void HandleDeleted(string slotId) => _onSlotDeleted?.Invoke(slotId);
    }
}
