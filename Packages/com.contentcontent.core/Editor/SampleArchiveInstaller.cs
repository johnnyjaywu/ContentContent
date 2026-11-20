using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ContentContent.Core.Editor.Samples
{
    /// <summary>
    /// Validates SHA-256 digests, extracts sample zip archives with Zip-Slip protection,
    /// copies local fallback samples, and manages <see cref="InstalledSampleMarker"/> metadata.
    /// </summary>
    public static class SampleArchiveInstaller
    {
        public const string InstallRootRelative = "Assets/Samples/ContentContent";
        public const string MarkerFileName = ".contentcontent-sample.json";

        public static string ComputeSha256Hex(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(data);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        public static bool VerifySha256(byte[] data, string expectedSha256)
        {
            if (data == null)
                return false;

            if (string.IsNullOrWhiteSpace(expectedSha256))
                return true;

            string actual = ComputeSha256Hex(data);
            return string.Equals(actual, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolveInstallDirectory(string projectRoot, string installSubPath)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Project root must not be empty.", nameof(projectRoot));
            if (string.IsNullOrWhiteSpace(installSubPath))
                throw new ArgumentException("Install sub-path must not be empty.", nameof(installSubPath));
            if (Path.IsPathRooted(installSubPath) || installSubPath.Contains(".."))
                throw new InvalidOperationException($"Unsafe install sub-path: '{installSubPath}'.");

            string rootPath = Path.GetFullPath(Path.Combine(projectRoot, InstallRootRelative.Replace('/', Path.DirectorySeparatorChar)));
            string rootWithSep = EnsureTrailingSeparator(rootPath);
            string normalizedSub = installSubPath.Trim('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            string combined = Path.GetFullPath(Path.Combine(rootPath, normalizedSub));

            if (!combined.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Resolved install path escapes '{InstallRootRelative}': '{combined}'.");

            return combined;
        }

        public static string ToUnityAssetPath(string installSubPath)
        {
            string normalized = (installSubPath ?? string.Empty).Trim('/', '\\').Replace('\\', '/');
            return $"{InstallRootRelative}/{normalized}";
        }

        public static void ExtractZipSafely(byte[] zipBytes, string destinationDirectory, bool cleanDestination = true)
        {
            if (zipBytes == null || zipBytes.Length == 0)
                throw new ArgumentException("Zip archive payload is empty.", nameof(zipBytes));
            if (string.IsNullOrWhiteSpace(destinationDirectory))
                throw new ArgumentException("Destination directory must not be empty.", nameof(destinationDirectory));

            string destFull = Path.GetFullPath(destinationDirectory);
            string destWithSep = EnsureTrailingSeparator(destFull);

            using (var stream = new MemoryStream(zipBytes, writable: false))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false))
            {
                // Pre-validate every entry path against Zip-Slip before modifying disk state
                for (int i = 0; i < archive.Entries.Count; i++)
                {
                    ZipArchiveEntry entry = archive.Entries[i];
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    string entryRel = entry.FullName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                    string targetPath = Path.GetFullPath(Path.Combine(destFull, entryRel));
                    if (!targetPath.StartsWith(destWithSep, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Archive contains an unsafe path traversal entry: '{entry.FullName}'.");
                    }
                }

                if (cleanDestination && Directory.Exists(destFull))
                {
                    Directory.Delete(destFull, recursive: true);
                }

                Directory.CreateDirectory(destFull);

                for (int i = 0; i < archive.Entries.Count; i++)
                {
                    ZipArchiveEntry entry = archive.Entries[i];
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    string entryRel = entry.FullName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                    string targetPath = Path.GetFullPath(Path.Combine(destFull, entryRel));
                    string parentDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }

                    using (Stream entryStream = entry.Open())
                    using (FileStream fileStream = File.Create(targetPath))
                    {
                        entryStream.CopyTo(fileStream);
                    }
                }
            }
        }

        public static void CopyDirectorySafely(string sourceDirectory, string destinationDirectory, bool cleanDestination = true)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException($"Sample source directory not found: '{sourceDirectory}'.");

            string srcFull = Path.GetFullPath(sourceDirectory);
            string srcWithSep = EnsureTrailingSeparator(srcFull);
            string destFull = Path.GetFullPath(destinationDirectory);

            if (cleanDestination && Directory.Exists(destFull))
            {
                Directory.Delete(destFull, recursive: true);
            }

            Directory.CreateDirectory(destFull);

            string[] files = Directory.GetFiles(srcFull, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string fileName = Path.GetFileName(file);
                if (fileName == ".DS_Store" || fileName == "Thumbs.db" || fileName == MarkerFileName)
                    continue;

                string rel = file.Substring(srcWithSep.Length);
                string target = Path.Combine(destFull, rel);
                string parent = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.Copy(file, target, overwrite: true);
            }
        }

        public static void WriteInstallMarker(string installDirectory, SampleManifestEntry entry)
        {
            if (string.IsNullOrWhiteSpace(installDirectory))
                throw new ArgumentException("Install directory must not be empty.", nameof(installDirectory));
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            Directory.CreateDirectory(installDirectory);
            var marker = new InstalledSampleMarker
            {
                packageName = entry.packageName ?? string.Empty,
                sampleId = entry.sampleId ?? string.Empty,
                version = entry.version ?? string.Empty,
                sha256 = entry.sha256 ?? string.Empty,
                installedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            string json = JsonUtility.ToJson(marker, prettyPrint: true);
            File.WriteAllText(Path.Combine(installDirectory, MarkerFileName), json, new UTF8Encoding(false));
        }

        public static bool TryReadInstallMarker(string installDirectory, out InstalledSampleMarker marker)
        {
            marker = null;
            if (string.IsNullOrWhiteSpace(installDirectory))
                return false;

            string markerPath = Path.Combine(installDirectory, MarkerFileName);
            if (!File.Exists(markerPath))
                return false;

            try
            {
                string json = File.ReadAllText(markerPath);
                marker = JsonUtility.FromJson<InstalledSampleMarker>(json);
                return marker != null && !string.IsNullOrEmpty(marker.sampleId);
            }
            catch
            {
                marker = null;
                return false;
            }
        }

        public static bool RemoveInstalledSample(string installDirectory, string stopAtRootDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
                return false;

            string dirFull = Path.GetFullPath(installDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Directory.Delete(dirFull, recursive: true);

            string metaPath = dirFull + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }

            if (!string.IsNullOrWhiteSpace(stopAtRootDirectory))
            {
                string stopFull = Path.GetFullPath(stopAtRootDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string parent = Path.GetDirectoryName(dirFull);
                while (!string.IsNullOrEmpty(parent) &&
                       parent.Length > stopFull.Length &&
                       parent.StartsWith(stopFull, StringComparison.OrdinalIgnoreCase) &&
                       Directory.Exists(parent) &&
                       Directory.GetFileSystemEntries(parent).Length == 0)
                {
                    Directory.Delete(parent, recursive: false);
                    string parentMeta = parent + ".meta";
                    if (File.Exists(parentMeta))
                    {
                        File.Delete(parentMeta);
                    }
                    parent = Path.GetDirectoryName(parent);
                }
            }

            return true;
        }

        public static string ResolveEntrySceneAssetPath(SampleManifestEntry entry, string installDirectory)
        {
            if (entry == null || string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
                return string.Empty;

            string baseSubPath = (entry.installSubPath ?? string.Empty).Trim('/', '\\').Replace('\\', '/');
            if (!string.IsNullOrWhiteSpace(entry.entryScene))
            {
                string relScene = entry.entryScene.Trim('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                string fullScene = Path.Combine(installDirectory, relScene);
                if (File.Exists(fullScene))
                {
                    return ToUnityAssetPath($"{baseSubPath}/{entry.entryScene.Trim('/', '\\').Replace('\\', '/')}");
                }
            }

            string[] scenes = Directory.GetFiles(installDirectory, "*.unity", SearchOption.AllDirectories);
            if (scenes.Length > 0)
            {
                string dirWithSep = EnsureTrailingSeparator(Path.GetFullPath(installDirectory));
                string relFound = Path.GetFullPath(scenes[0]).Substring(dirWithSep.Length).Replace('\\', '/');
                return ToUnityAssetPath($"{baseSubPath}/{relFound}");
            }

            return string.Empty;
        }

        public static string FormatByteSize(long bytes)
        {
            if (bytes <= 0)
                return "Local";
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:0.#} KB";
            return $"{bytes / (1024.0 * 1024.0):0.##} MB";
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString()) || path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                return path;
            return path + Path.DirectorySeparatorChar;
        }
    }
}
