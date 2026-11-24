using System.Collections.Generic;

namespace ContentContent.Save
{
    /// <summary>
    /// Implemented by gameplay systems or entities that participate in save/load cycles.
    /// </summary>
    public interface ISaveable
    {
        string SaveId { get; }
        void CaptureState(SaveSection section);
        void RestoreState(SaveSection section);
    }

    /// <summary>
    /// Deterministic schema upgrade step transforming a <see cref="SaveDocument"/> from
    /// <see cref="FromVersion"/> to <see cref="ToVersion"/> before state is restored.
    /// </summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        void Migrate(SaveDocument document);
    }

    /// <summary>
    /// Storage backend abstraction (in-memory for tests/cloud hooks or disk files with atomic writes).
    /// </summary>
    public interface ISaveStorage
    {
        bool Exists(string slotId);
        bool TryRead(string slotId, out string payload);
        void WriteAtomic(string slotId, string payload);
        bool Delete(string slotId);
        void ListSlots(List<string> outSlots);
    }
}
