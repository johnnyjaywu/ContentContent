using System;
using System.Collections.Generic;

namespace ContentContent.Save
{
    /// <summary>
    /// Coordinates save/load orchestration across registered <see cref="ISaveable"/> participants,
    /// deterministic <see cref="ISaveMigration"/> chains, and an <see cref="ISaveStorage"/> backend.
    /// </summary>
    public sealed class SaveManager
    {
        private readonly Dictionary<string, ISaveable> _participants = new Dictionary<string, ISaveable>(StringComparer.Ordinal);
        private readonly Dictionary<int, ISaveMigration> _migrationsBySourceVersion = new Dictionary<int, ISaveMigration>();
        private readonly List<string> _slotScratch = new List<string>(16);

        public int CurrentSchemaVersion { get; }
        public ISaveStorage Storage { get; }
        public int ParticipantCount => _participants.Count;

        public event Action<SaveDocument> OnBeforeSave;
        public event Action<SaveDocument> OnAfterSave;
        public event Action<string> OnBeforeLoad;
        public event Action<SaveDocument> OnAfterLoad;
        public event Action<string> OnSaveDeleted;
        public event Action<int, int> OnMigrationApplied;

        public SaveManager(ISaveStorage storage, int currentSchemaVersion = 1)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            CurrentSchemaVersion = Math.Max(1, currentSchemaVersion);
        }

        public void Register(ISaveable saveable)
        {
            if (saveable == null)
                throw new ArgumentNullException(nameof(saveable));
            if (string.IsNullOrWhiteSpace(saveable.SaveId))
                throw new ArgumentException("ISaveable.SaveId must not be empty.", nameof(saveable));

            _participants[saveable.SaveId] = saveable;
        }

        public bool Unregister(ISaveable saveable)
        {
            if (saveable == null || string.IsNullOrEmpty(saveable.SaveId))
                return false;

            if (_participants.TryGetValue(saveable.SaveId, out ISaveable existing) && ReferenceEquals(existing, saveable))
                return _participants.Remove(saveable.SaveId);

            return false;
        }

        public void RegisterMigration(ISaveMigration migration)
        {
            if (migration == null)
                throw new ArgumentNullException(nameof(migration));
            if (migration.ToVersion <= migration.FromVersion)
                throw new ArgumentException("Migration ToVersion must be greater than FromVersion.", nameof(migration));

            _migrationsBySourceVersion[migration.FromVersion] = migration;
        }

        public SaveDocument CaptureDocument(
            string slotId,
            string displayName = null,
            float playTimeSeconds = 0f,
            string locationLabel = null)
        {
            string safeSlot = SaveSlotUtility.ValidateSlotId(slotId);
            var doc = new SaveDocument(CurrentSchemaVersion)
            {
                Metadata = new SaveMetadata
                {
                    SlotId = safeSlot,
                    DisplayName = string.IsNullOrEmpty(displayName) ? safeSlot : displayName,
                    SchemaVersion = CurrentSchemaVersion,
                    SavedAtUtcTicks = DateTime.UtcNow.Ticks,
                    PlayTimeSeconds = Math.Max(0f, playTimeSeconds),
                    LocationLabel = locationLabel ?? string.Empty
                }
            };

            foreach (KeyValuePair<string, ISaveable> kvp in _participants)
            {
                SaveSection section = doc.GetOrCreateSection(kvp.Key);
                kvp.Value.CaptureState(section);
            }

            return doc;
        }

        public SaveDocument SaveSlot(
            string slotId,
            string displayName = null,
            float playTimeSeconds = 0f,
            string locationLabel = null)
        {
            SaveDocument doc = CaptureDocument(slotId, displayName, playTimeSeconds, locationLabel);
            OnBeforeSave?.Invoke(doc);

            string json = SaveJsonCodec.Encode(doc);
            Storage.WriteAtomic(doc.Metadata.SlotId, json);

            OnAfterSave?.Invoke(doc);
            return doc;
        }

        public SaveDocument SaveAutosave(
            string autosaveSlotId = "autosave",
            string displayName = "Autosave",
            float playTimeSeconds = 0f,
            string locationLabel = null)
        {
            return SaveSlot(autosaveSlotId, displayName, playTimeSeconds, locationLabel);
        }

        public bool TryLoadSlot(string slotId, out SaveDocument loadedDocument)
        {
            string safeSlot = SaveSlotUtility.ValidateSlotId(slotId);
            OnBeforeLoad?.Invoke(safeSlot);

            if (!TryDecodeSlotDocument(safeSlot, out SaveDocument doc))
            {
                loadedDocument = null;
                return false;
            }

            if (!TryApplyMigrations(doc))
            {
                loadedDocument = null;
                return false;
            }

            foreach (KeyValuePair<string, ISaveable> kvp in _participants)
            {
                if (doc.TryGetSection(kvp.Key, out SaveSection section))
                {
                    kvp.Value.RestoreState(section);
                }
            }

            loadedDocument = doc;
            OnAfterLoad?.Invoke(doc);
            return true;
        }

        public bool TryReadMetadata(string slotId, out SaveMetadata metadata)
        {
            string safeSlot = SaveSlotUtility.ValidateSlotId(slotId);
            if (!TryDecodeSlotDocument(safeSlot, out SaveDocument doc))
            {
                metadata = default;
                return false;
            }

            metadata = doc.Metadata;
            return true;
        }

        public bool SlotExists(string slotId) => Storage.Exists(slotId);

        public bool DeleteSlot(string slotId)
        {
            string safe = SaveSlotUtility.ValidateSlotId(slotId);
            bool deleted = Storage.Delete(safe);
            if (deleted)
            {
                OnSaveDeleted?.Invoke(safe);
            }
            return deleted;
        }

        public void ListSlots(List<string> outSlots) => Storage.ListSlots(outSlots);

        public List<SaveMetadata> ListSlotMetadata()
        {
            Storage.ListSlots(_slotScratch);
            var result = new List<SaveMetadata>(_slotScratch.Count);
            for (int i = 0; i < _slotScratch.Count; i++)
            {
                if (TryReadMetadata(_slotScratch[i], out SaveMetadata meta))
                {
                    result.Add(meta);
                }
            }
            return result;
        }

        private bool TryDecodeSlotDocument(string safeSlotId, out SaveDocument document)
        {
            if (Storage.TryRead(safeSlotId, out string primaryPayload) &&
                SaveJsonCodec.TryDecode(primaryPayload, out document))
            {
                return true;
            }

            if (Storage is FileSaveStorage fileStorage &&
                fileStorage.TryReadBackup(safeSlotId, out string fileBackup) &&
                SaveJsonCodec.TryDecode(fileBackup, out document))
            {
                return true;
            }

            if (Storage is InMemorySaveStorage memStorage &&
                memStorage.TryReadBackup(safeSlotId, out string memBackup) &&
                SaveJsonCodec.TryDecode(memBackup, out document))
            {
                return true;
            }

            document = null;
            return false;
        }

        private bool TryApplyMigrations(SaveDocument document)
        {
            int version = Math.Max(1, document.SchemaVersion);
            if (version > CurrentSchemaVersion)
                return false;

            int safety = 0;
            while (version < CurrentSchemaVersion && safety < 256)
            {
                if (!_migrationsBySourceVersion.TryGetValue(version, out ISaveMigration migration))
                    return false;

                int from = version;
                migration.Migrate(document);
                version = migration.ToVersion;
                document.SchemaVersion = version;

                SaveMetadata meta = document.Metadata;
                meta.SchemaVersion = version;
                document.Metadata = meta;

                OnMigrationApplied?.Invoke(from, version);
                safety++;
            }

            return version == CurrentSchemaVersion;
        }
    }
}
