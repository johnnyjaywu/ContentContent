# Save System Scripting API

Complete reference for `ContentContent.Save` (pure C# core) and `ContentContent.Save.Unity` (Unity adapters).

---

## Namespace: `ContentContent.Save`

### `SaveValueType` & `SaveValue`

- `SaveValueType`: `None = 0`, `Bool = 1`, `Int = 2`, `Long = 3`, `Float = 4`, `Double = 5`, `String = 6`.
- `SaveValue`: immutable union with factories (`FromBool`, `FromInt`, `FromLong`, `FromFloat`, `FromDouble`, `FromString`) and converters (`AsBool`, `AsInt`, `AsLong`, `AsFloat`, `AsDouble`, `AsString`).

### `SaveSection`, `SaveMetadata` & `SaveDocument`

- `SaveSection`: typed dictionary wrapper with `SetBool/Int/Long/Float/Double/String/Value`, `GetBool/Int/Long/Float/Double/String`, `TryGet`, `Has`, `Remove`, `Clear`, `Clone()`.
- `SaveMetadata`: `SlotId`, `DisplayName`, `SchemaVersion`, `SavedAtUtcTicks`, `PlayTimeSeconds`, `LocationLabel`.
- `SaveDocument`: `SchemaVersion`, `Metadata`, `Sections`, `GetOrCreateSection`, `TryGetSection`, `SetSection`, `RemoveSection`, `Clone()`.

### Contracts & Storage

- `ISaveable`: `SaveId`, `CaptureState(SaveSection)`, `RestoreState(SaveSection)`.
- `ISaveMigration`: `FromVersion`, `ToVersion`, `Migrate(SaveDocument)`.
- `ISaveStorage`: `Exists`, `TryRead`, `WriteAtomic`, `Delete`, `ListSlots`.
- `InMemorySaveStorage` & `FileSaveStorage(string directoryPath)`: safe slot validation (`SaveSlotUtility.ValidateSlotId`), `.json.tmp` -> `.json.bak` -> `.json` atomic replacement.
- `SaveJsonCodec`: static `Encode(SaveDocument)`, `Decode(string)`, `TryDecode(string, out SaveDocument)`.

### `SaveManager` (`sealed class`)

```csharp
public SaveManager(ISaveStorage storage, int currentSchemaVersion = 1)
```

- Properties: `CurrentSchemaVersion`, `Storage`, `ParticipantCount`.
- Methods: `Register(ISaveable)`, `Unregister(ISaveable)`, `RegisterMigration(ISaveMigration)`, `CaptureDocument(...)`, `SaveSlot(...)`, `SaveAutosave(...)`, `TryLoadSlot(slotId, out SaveDocument)`, `TryReadMetadata(slotId, out SaveMetadata)`, `SlotExists(slotId)`, `DeleteSlot(slotId)`, `ListSlots(List<string>)`, `ListSlotMetadata()`.
- Events: `OnBeforeSave`, `OnAfterSave`, `OnBeforeLoad`, `OnAfterLoad`, `OnSaveDeleted`, `OnMigrationApplied`.

---

## Namespace: `ContentContent.Save.Unity`

- **`SaveManagerBehaviour`** (`MonoBehaviour`): Owns `SaveManager`, resolves `Application.persistentDataPath/<subdir>` (or `InMemorySaveStorage`), tracks playtime and autosave interval, and exposes `SaveDefaultSlot()`, `LoadDefaultSlot()`, `SaveSlot(string)`, `LoadSlot(string)`, `SaveAutosave()`, `DeleteSlot(string)`, `SlotExists(string)`, plus `OnSlotSaved`, `OnSlotLoaded`, `OnSlotDeleted`, `OnLoadFailed` `UnityEvent<string>` hooks.
- **`SaveableBehaviour`** (`abstract MonoBehaviour`, `ISaveable`): Auto-registers on `Awake`/`OnEnable` and unregisters on `OnDestroy` so deactivated GameObjects keep participating.
- **`SaveableTransform`** (`SaveableBehaviour`): Captures/restores local position, rotation, scale, and `activeSelf`.
