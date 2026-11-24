#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using ContentContent.Core.Editor.Samples;
using NUnit.Framework;
using UnityEngine;

namespace ContentContent.Core.Tests
{
    public class SampleDownloaderEditorTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ContentContent_SampleTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_tempDir) && Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        [Test]
        public void SampleManifest_DeserializesExpectedFields()
        {
            const string json = @"{
                ""schemaVersion"": 1,
                ""generatedAtUtc"": ""2026-11-19T00:00:00Z"",
                ""releaseTag"": ""samples-latest"",
                ""samples"": [
                    {
                        ""packageName"": ""com.contentcontent.core"",
                        ""sampleId"": ""core-playground"",
                        ""displayName"": ""Core Playground"",
                        ""description"": ""Demo"",
                        ""version"": ""1.0.0"",
                        ""installSubPath"": ""Core/CorePlayground"",
                        ""entryScene"": ""CorePlayground.unity"",
                        ""archiveFileName"": ""core-playground.zip"",
                        ""downloadUrl"": ""https://example.com/core-playground.zip"",
                        ""sha256"": ""abcdef"",
                        ""sizeBytes"": 1024
                    }
                ]
            }";

            SampleManifest manifest = JsonUtility.FromJson<SampleManifest>(json);
            Assert.IsNotNull(manifest);
            Assert.AreEqual(1, manifest.schemaVersion);
            Assert.AreEqual("samples-latest", manifest.releaseTag);
            Assert.IsNotNull(manifest.samples);
            Assert.AreEqual(1, manifest.samples.Length);
            Assert.AreEqual("core-playground", manifest.samples[0].sampleId);
            Assert.AreEqual("CorePlayground.unity", manifest.samples[0].entryScene);
            Assert.AreEqual(1024L, manifest.samples[0].sizeBytes);
        }

        [Test]
        public void SampleArchiveInstaller_VerifiesSha256AndRejectsTamperedData()
        {
            byte[] payload = Encoding.UTF8.GetBytes("ContentContentSamplePayload");
            string hex = SampleArchiveInstaller.ComputeSha256Hex(payload);

            Assert.AreEqual(64, hex.Length);
            Assert.IsTrue(SampleArchiveInstaller.VerifySha256(payload, hex));
            Assert.IsTrue(SampleArchiveInstaller.VerifySha256(payload, hex.ToUpperInvariant()));
            Assert.IsFalse(SampleArchiveInstaller.VerifySha256(Encoding.UTF8.GetBytes("Tampered"), hex));
        }

        [Test]
        public void SampleArchiveInstaller_ExtractsValidZipAndRejectsZipSlipTraversal()
        {
            byte[] validZip = BuildZipBytes(
                ("Sample.cs", "class Sample {}"),
                ("Sample.cs.meta", "fileFormatVersion: 2\nguid: 00000000000000000000000000000001\n"),
                ("Sub/README.md", "# Sample"));
            string installDir = SampleArchiveInstaller.ResolveInstallDirectory(_tempDir, "Core/CorePlayground");

            SampleArchiveInstaller.ExtractZipSafely(validZip, installDir);
            Assert.IsTrue(File.Exists(Path.Combine(installDir, "Sample.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(installDir, "Sample.cs.meta")));
            Assert.IsTrue(File.Exists(Path.Combine(installDir, "Sub", "README.md")));

            byte[] unsafeZip = BuildZipBytes(("../../escaped.txt", "pwned"));
            string evilTarget = Path.Combine(_tempDir, "EvilTarget");
            Assert.Throws<InvalidOperationException>(() =>
                SampleArchiveInstaller.ExtractZipSafely(unsafeZip, evilTarget));

            Assert.Throws<InvalidOperationException>(() =>
                SampleArchiveInstaller.ResolveInstallDirectory(_tempDir, "../Escape"));
        }

        [Test]
        public void SampleArchiveInstaller_CopyPreservesMetaResolvesSceneAndCleansEmptyParents()
        {
            string srcDir = Path.Combine(_tempDir, "Src");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "Demo.cs"), "// demo");
            File.WriteAllText(Path.Combine(srcDir, "Demo.cs.meta"), "guid: 1234");
            File.WriteAllText(Path.Combine(srcDir, "DemoScene.unity"), "%YAML 1.1");
            File.WriteAllText(Path.Combine(srcDir, "DemoScene.unity.meta"), "guid: 5678");

            string installRoot = Path.Combine(_tempDir, SampleArchiveInstaller.InstallRootRelative.Replace('/', Path.DirectorySeparatorChar));
            string dstDir = SampleArchiveInstaller.ResolveInstallDirectory(_tempDir, "Stats/CombatStats");
            SampleArchiveInstaller.CopyDirectorySafely(srcDir, dstDir);

            Assert.IsTrue(File.Exists(Path.Combine(dstDir, "Demo.cs.meta")));
            Assert.IsTrue(File.Exists(Path.Combine(dstDir, "DemoScene.unity.meta")));

            var entry = new SampleManifestEntry
            {
                packageName = "com.contentcontent.stats",
                sampleId = "combat-stats",
                version = "1.0.0",
                installSubPath = "Stats/CombatStats",
                entryScene = "DemoScene.unity",
                sha256 = "abc123"
            };

            string sceneAssetPath = SampleArchiveInstaller.ResolveEntrySceneAssetPath(entry, dstDir);
            Assert.AreEqual("Assets/Samples/ContentContent/Stats/CombatStats/DemoScene.unity", sceneAssetPath);

            SampleArchiveInstaller.WriteInstallMarker(dstDir, entry);
            Assert.IsTrue(SampleArchiveInstaller.TryReadInstallMarker(dstDir, out InstalledSampleMarker marker));
            Assert.AreEqual("combat-stats", marker.sampleId);
            Assert.AreEqual("1.0.0", marker.version);

            Assert.IsTrue(SampleArchiveInstaller.RemoveInstalledSample(dstDir, installRoot));
            Assert.IsFalse(Directory.Exists(dstDir));
            Assert.IsFalse(Directory.Exists(Path.Combine(installRoot, "Stats")));
        }

        [Test]
        public void SampleDownloadService_LocalCatalogEntriesHavePlayableSceneWithMatchingMetaGuid()
        {
            SampleManifest local = SampleDownloadService.TryLoadLocalCatalog();
            Assert.IsNotNull(local);
            Assert.IsNotNull(local.samples);
            Assert.GreaterOrEqual(local.samples.Length, 7);

            Assert.AreEqual(1, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.core").Count);
            Assert.AreEqual(1, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.navigation").Count);
            Assert.AreEqual(1, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.stats").Count);
            Assert.AreEqual(2, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.dialog").Count);
            Assert.AreEqual(1, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.character").Count);
            Assert.AreEqual(1, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.world").Count);

            var seenSampleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var guidRegex = new Regex(@"^guid:\s*([0-9a-f]{32})", RegexOptions.Multiline);
            for (int i = 0; i < local.samples.Length; i++)
            {
                SampleManifestEntry entry = local.samples[i];
                Assert.IsTrue(seenSampleIds.Add(entry.sampleId), $"Duplicate sampleId '{entry.sampleId}'");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.entryScene), $"Missing entryScene for {entry.sampleId}");
                Assert.IsTrue(Directory.Exists(entry.LocalSourceDirectory), $"Missing source directory for {entry.sampleId}");

                string scenePath = Path.Combine(entry.LocalSourceDirectory, entry.entryScene);
                Assert.IsTrue(File.Exists(scenePath), $"Missing scene '{scenePath}' for {entry.sampleId}");
                Assert.IsTrue(File.Exists(scenePath + ".meta"), $"Missing scene .meta for {entry.sampleId}");

                var scriptGuids = new List<string>();
                foreach (string csFile in Directory.GetFiles(entry.LocalSourceDirectory, "*.cs", SearchOption.AllDirectories))
                {
                    string metaPath = csFile + ".meta";
                    Assert.IsTrue(File.Exists(metaPath), $"Missing .meta for sample script '{csFile}'");
                    Match match = guidRegex.Match(File.ReadAllText(metaPath));
                    Assert.IsTrue(match.Success, $"Invalid GUID in '{metaPath}'");
                    scriptGuids.Add(match.Groups[1].Value);
                }

                string sceneText = File.ReadAllText(scenePath);
                bool referencesSampleScript = false;
                for (int g = 0; g < scriptGuids.Count; g++)
                {
                    if (sceneText.Contains(scriptGuids[g]))
                    {
                        referencesSampleScript = true;
                        break;
                    }
                }

                Assert.IsTrue(referencesSampleScript, $"Scene '{entry.entryScene}' does not reference any sample script GUID.");
            }
        }

        private static byte[] BuildZipBytes(params (string EntryName, string Content)[] entries)
        {
            using (var ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                {
                    for (int i = 0; i < entries.Length; i++)
                    {
                        ZipArchiveEntry e = archive.CreateEntry(entries[i].EntryName);
                        using (var writer = new StreamWriter(e.Open(), Encoding.UTF8))
                        {
                            writer.Write(entries[i].Content);
                        }
                    }
                }

                return ms.ToArray();
            }
        }
    }
}
#endif
