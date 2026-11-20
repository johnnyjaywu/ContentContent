using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ContentContent.Core.Editor.Samples
{
    /// <summary>
    /// Fetches the remote <c>samples-manifest.json</c> (falling back to local <c>Samples/samples-catalog.json</c>
    /// inside the source repository), downloads and verifies sample <c>.zip</c> archives via <see cref="HttpClient"/>,
    /// and installs them into <c>Assets/Samples/ContentContent/</c>.
    /// </summary>
    public static class SampleDownloadService
    {
        public const string DefaultManifestUrl =
            "https://github.com/johnnyjaywu/ContentContent/releases/download/samples-latest/samples-manifest.json";

        public const string ManifestUrlPrefKey = "ContentContent.Samples.ManifestUrl";

        private static readonly HttpClient s_httpClient = CreateHttpClient();

        public static SampleManifest CachedManifest { get; private set; }
        public static bool IsUsingLocalFallback { get; private set; }
        public static string LastError { get; private set; } = string.Empty;

        public static string ManifestUrl
        {
            get
            {
                string saved = EditorPrefs.GetString(ManifestUrlPrefKey, DefaultManifestUrl);
                return string.IsNullOrWhiteSpace(saved) ? DefaultManifestUrl : saved;
            }
            set => EditorPrefs.SetString(ManifestUrlPrefKey, string.IsNullOrWhiteSpace(value) ? DefaultManifestUrl : value.Trim());
        }

        public static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        public static SampleManifest TryLoadLocalCatalog(string projectRoot = null)
        {
            string root = string.IsNullOrWhiteSpace(projectRoot) ? GetProjectRoot() : projectRoot;
            string samplesRoot = Path.Combine(root, "Samples");
            string catalogPath = Path.Combine(samplesRoot, "samples-catalog.json");
            if (!File.Exists(catalogPath))
                return null;

            string json = File.ReadAllText(catalogPath);
            LocalSampleCatalog catalog = JsonUtility.FromJson<LocalSampleCatalog>(json);
            if (catalog == null || catalog.samples == null)
                return null;

            var entries = new SampleManifestEntry[catalog.samples.Length];
            for (int i = 0; i < catalog.samples.Length; i++)
            {
                LocalSampleCatalogEntry src = catalog.samples[i];
                string relDir = (src.sourcePath ?? string.Empty).Replace('/', Path.DirectorySeparatorChar);
                string localDir = Path.GetFullPath(Path.Combine(samplesRoot, relDir));

                entries[i] = new SampleManifestEntry
                {
                    packageName = src.packageName,
                    sampleId = src.sampleId,
                    displayName = src.displayName,
                    description = src.description,
                    version = src.version,
                    installSubPath = src.installSubPath,
                    entryScene = src.entryScene,
                    archiveFileName = $"{src.sampleId}.zip",
                    downloadUrl = string.Empty,
                    sha256 = string.Empty,
                    sizeBytes = ComputeDirectorySize(localDir),
                    LocalSourceDirectory = localDir
                };
            }

            return new SampleManifest
            {
                schemaVersion = catalog.schemaVersion,
                generatedAtUtc = string.Empty,
                releaseTag = catalog.defaultReleaseTag,
                samples = entries
            };
        }

        public static async Task<SampleManifest> FetchManifestAsync(bool forceRefresh = false, CancellationToken ct = default)
        {
            if (!forceRefresh && CachedManifest != null)
                return CachedManifest;

            LastError = string.Empty;

            try
            {
                using (HttpResponseMessage response = await s_httpClient.GetAsync(ManifestUrl, ct))
                {
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync();
                    SampleManifest remote = JsonUtility.FromJson<SampleManifest>(json);

                    if (remote != null && remote.samples != null && remote.samples.Length > 0)
                    {
                        CachedManifest = remote;
                        IsUsingLocalFallback = false;
                        return CachedManifest;
                    }

                    LastError = "Remote samples manifest is empty or invalid.";
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }

            SampleManifest local = TryLoadLocalCatalog();
            if (local != null)
            {
                CachedManifest = local;
                IsUsingLocalFallback = true;
                LastError = string.Empty;
                return CachedManifest;
            }

            return null;
        }

        public static List<SampleManifestEntry> GetSamplesForPackage(SampleManifest manifest, string packageName)
        {
            var result = new List<SampleManifestEntry>();
            if (manifest == null || manifest.samples == null || string.IsNullOrEmpty(packageName))
                return result;

            for (int i = 0; i < manifest.samples.Length; i++)
            {
                SampleManifestEntry entry = manifest.samples[i];
                if (entry != null && string.Equals(entry.packageName, packageName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        public static bool IsInstalled(SampleManifestEntry entry, out InstalledSampleMarker marker, out string installDir)
        {
            marker = null;
            installDir = string.Empty;
            if (entry == null || string.IsNullOrEmpty(entry.installSubPath))
                return false;

            installDir = SampleArchiveInstaller.ResolveInstallDirectory(GetProjectRoot(), entry.installSubPath);
            if (!Directory.Exists(installDir))
                return false;

            SampleArchiveInstaller.TryReadInstallMarker(installDir, out marker);
            return true;
        }

        public static async Task<string> DownloadAndInstallAsync(
            SampleManifestEntry entry,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            string installDir = SampleArchiveInstaller.ResolveInstallDirectory(GetProjectRoot(), entry.installSubPath);

            if (!string.IsNullOrEmpty(entry.LocalSourceDirectory) && Directory.Exists(entry.LocalSourceDirectory))
            {
                SampleArchiveInstaller.CopyDirectorySafely(entry.LocalSourceDirectory, installDir, cleanDestination: true);
                progress?.Report(1f);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(entry.downloadUrl))
                    throw new InvalidOperationException($"No downloadUrl specified for sample '{entry.sampleId}'.");

                byte[] zipBytes = await DownloadBytesAsync(entry.downloadUrl, progress, ct);
                if (!SampleArchiveInstaller.VerifySha256(zipBytes, entry.sha256))
                {
                    throw new InvalidOperationException(
                        $"SHA-256 verification failed for '{entry.archiveFileName}'. Expected '{entry.sha256}'.");
                }

                SampleArchiveInstaller.ExtractZipSafely(zipBytes, installDir, cleanDestination: true);
                progress?.Report(1f);
            }

            SampleArchiveInstaller.WriteInstallMarker(installDir, entry);
            AssetDatabase.Refresh();
            return SampleArchiveInstaller.ToUnityAssetPath(entry.installSubPath);
        }

        public static void RemoveSample(SampleManifestEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.installSubPath))
                return;

            string projectRoot = GetProjectRoot();
            string installRoot = Path.Combine(projectRoot, SampleArchiveInstaller.InstallRootRelative.Replace('/', Path.DirectorySeparatorChar));
            string installDir = SampleArchiveInstaller.ResolveInstallDirectory(projectRoot, entry.installSubPath);
            if (SampleArchiveInstaller.RemoveInstalledSample(installDir, installRoot))
            {
                AssetDatabase.Refresh();
            }
        }

        private static async Task<byte[]> DownloadBytesAsync(string url, IProgress<float> progress, CancellationToken ct)
        {
            using (HttpResponseMessage response = await s_httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                long? contentLength = response.Content.Headers.ContentLength;

                using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                using (var bufferStream = new MemoryStream(contentLength.HasValue && contentLength.Value > 0 && contentLength.Value < int.MaxValue ? (int)contentLength.Value : 16384))
                {
                    byte[] chunk = new byte[16384];
                    long totalRead = 0;
                    int read;
                    while ((read = await responseStream.ReadAsync(chunk, 0, chunk.Length, ct)) > 0)
                    {
                        bufferStream.Write(chunk, 0, read);
                        totalRead += read;
                        if (contentLength.HasValue && contentLength.Value > 0)
                        {
                            progress?.Report(Mathf.Clamp01((float)totalRead / contentLength.Value) * 0.9f);
                        }
                    }

                    return bufferStream.ToArray();
                }
            }
        }

        private static long ComputeDirectorySize(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return 0;

            long total = 0;
            string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                if (fileName != ".DS_Store" && fileName != "Thumbs.db" && fileName != SampleArchiveInstaller.MarkerFileName)
                {
                    total += new FileInfo(files[i]).Length;
                }
            }

            return total;
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ContentContent-Unity-Samples/1.0");
            return client;
        }
    }
}
