using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ContentContent.Save
{
    public static class SaveSlotUtility
    {
        /// <summary>
        /// Validates that <paramref name="slotId"/> is a safe single-segment identifier ([a-zA-Z0-9_-]),
        /// preventing directory traversal or invalid filename characters across operating systems.
        /// </summary>
        public static string ValidateSlotId(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
                throw new ArgumentException("Save slot id must not be empty.", nameof(slotId));

            string trimmed = slotId.Trim();
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                bool valid = (c >= 'a' && c <= 'z') ||
                             (c >= 'A' && c <= 'Z') ||
                             (c >= '0' && c <= '9') ||
                             c == '_' ||
                             c == '-';
                if (!valid)
                {
                    throw new ArgumentException(
                        $"Invalid save slot id '{slotId}'. Only ASCII letters, digits, '_' and '-' are allowed.",
                        nameof(slotId));
                }
            }

            return trimmed;
        }
    }

    /// <summary>
    /// Non-persistent storage backend ideal for headless unit tests or custom cloud wrappers.
    /// </summary>
    public sealed class InMemorySaveStorage : ISaveStorage
    {
        private readonly Dictionary<string, string> _slots = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _backups = new Dictionary<string, string>(StringComparer.Ordinal);

        public bool Exists(string slotId)
        {
            string safe = SaveSlotUtility.ValidateSlotId(slotId);
            return _slots.ContainsKey(safe) || _backups.ContainsKey(safe);
        }

        public bool TryRead(string slotId, out string payload)
        {
            string safe = SaveSlotUtility.ValidateSlotId(slotId);
            return _slots.TryGetValue(safe, out payload);
        }

        public bool TryReadBackup(string slotId, out string payload)
        {
            string safe = SaveSlotUtility.ValidateSlotId(slotId);
            return _backups.TryGetValue(safe, out payload);
        }

        public void WriteAtomic(string slotId, string payload)
        {
            string safe = SaveSlotUtility.ValidateSlotId(slotId);
            if (_slots.TryGetValue(safe, out string prev))
            {
                _backups[safe] = prev;
            }
            _slots[safe] = payload ?? string.Empty;
        }

        public bool Delete(string slotId)
        {
            string safe = SaveSlotUtility.ValidateSlotId(slotId);
            bool removedMain = _slots.Remove(safe);
            bool removedBak = _backups.Remove(safe);
            return removedMain || removedBak;
        }

        public void ListSlots(List<string> outSlots)
        {
            if (outSlots == null)
                throw new ArgumentNullException(nameof(outSlots));

            outSlots.Clear();
            foreach (string key in _slots.Keys)
            {
                outSlots.Add(key);
            }
            outSlots.Sort(StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// Atomic disk-backed save storage using temporary write (<c>.json.tmp</c>),
    /// previous-version backup (<c>.json.bak</c>), and safe replacement.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private const string MainExt = ".json";
        private const string TempExt = ".json.tmp";
        private const string BackupExt = ".json.bak";
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        public string DirectoryPath { get; }

        public FileSaveStorage(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Save directory path must not be empty.", nameof(directoryPath));

            DirectoryPath = Path.GetFullPath(directoryPath);
        }

        public string GetSlotFilePath(string slotId)
        {
            string safe = SaveSlotUtility.ValidateSlotId(slotId);
            return Path.Combine(DirectoryPath, safe + MainExt);
        }

        public string GetBackupFilePath(string slotId)
        {
            string safe = SaveSlotUtility.ValidateSlotId(slotId);
            return Path.Combine(DirectoryPath, safe + BackupExt);
        }

        public bool Exists(string slotId)
        {
            string main = GetSlotFilePath(slotId);
            string bak = GetBackupFilePath(slotId);
            return File.Exists(main) || File.Exists(bak);
        }

        public bool TryRead(string slotId, out string payload)
        {
            string main = GetSlotFilePath(slotId);
            if (TryReadFileText(main, out payload))
                return true;

            string bak = GetBackupFilePath(slotId);
            return TryReadFileText(bak, out payload);
        }

        public bool TryReadBackup(string slotId, out string payload)
        {
            string bak = GetBackupFilePath(slotId);
            return TryReadFileText(bak, out payload);
        }

        public void WriteAtomic(string slotId, string payload)
        {
            string safe = SaveSlotUtility.ValidateSlotId(slotId);
            Directory.CreateDirectory(DirectoryPath);

            string mainPath = Path.Combine(DirectoryPath, safe + MainExt);
            string tempPath = Path.Combine(DirectoryPath, safe + TempExt);
            string backupPath = Path.Combine(DirectoryPath, safe + BackupExt);

            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Utf8NoBom))
            {
                writer.Write(payload ?? string.Empty);
                writer.Flush();
                stream.Flush(true);
            }

            if (File.Exists(mainPath))
            {
                File.Copy(mainPath, backupPath, overwrite: true);
                File.Delete(mainPath);
            }

            File.Move(tempPath, mainPath);
        }

        public bool Delete(string slotId)
        {
            string safe = SaveSlotUtility.ValidateSlotId(slotId);
            string main = Path.Combine(DirectoryPath, safe + MainExt);
            string bak = Path.Combine(DirectoryPath, safe + BackupExt);
            string tmp = Path.Combine(DirectoryPath, safe + TempExt);

            bool deleted = false;
            if (File.Exists(main))
            {
                File.Delete(main);
                deleted = true;
            }
            if (File.Exists(bak))
            {
                File.Delete(bak);
                deleted = true;
            }
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }
            return deleted;
        }

        public void ListSlots(List<string> outSlots)
        {
            if (outSlots == null)
                throw new ArgumentNullException(nameof(outSlots));

            outSlots.Clear();
            if (!Directory.Exists(DirectoryPath))
                return;

            string[] files = Directory.GetFiles(DirectoryPath, "*" + MainExt, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                if (!string.IsNullOrEmpty(name))
                    outSlots.Add(name);
            }
            outSlots.Sort(StringComparer.Ordinal);
        }

        private static bool TryReadFileText(string path, out string content)
        {
            if (!File.Exists(path))
            {
                content = null;
                return false;
            }

            try
            {
                content = File.ReadAllText(path, Utf8NoBom);
                return true;
            }
            catch (IOException)
            {
                content = null;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                content = null;
                return false;
            }
        }
    }
}
