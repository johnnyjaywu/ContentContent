#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
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
            byte[] validZip = BuildZipBytes(("Sample.cs", "class Sample {}"), ("Sub/README.md", "# Sample"));
            string installDir = SampleArchiveInstaller.ResolveInstallDirectory(_tempDir, "Core/CorePlayground");

            SampleArchiveInstaller.ExtractZipSafely(validZip, installDir);
            Assert.IsTrue(File.Exists(Path.Combine(installDir, "Sample.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(installDir, "Sub", "README.md")));

            byte[] unsafeZip = BuildZipBytes(("../../escaped.txt", "pwned"));
            string evilTarget = Path.Combine(_tempDir, "EvilTarget");
            Assert.Throws<InvalidOperationException>(() =>
                SampleArchiveInstaller.ExtractZipSafely(unsafeZip, evilTarget));

            Assert.Throws<InvalidOperationException>(() =>
                SampleArchiveInstaller.ResolveInstallDirectory(_tempDir, "../Escape"));
        }

        [Test]
        public void SampleArchiveInstaller_LocalCopyWritesMarkerAndRemovesCleanly()
        {
            string srcDir = Path.Combine(_tempDir, "Src");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "Demo.cs"), "// demo");

            string dstDir = SampleArchiveInstaller.ResolveInstallDirectory(_tempDir, "Stats/CombatStats");
            SampleArchiveInstaller.CopyDirectorySafely(srcDir, dstDir);

            var entry = new SampleManifestEntry
            {
                packageName = "com.contentcontent.stats",
                sampleId = "combat-stats",
                version = "1.0.0",
                sha256 = "abc123"
            };
            SampleArchiveInstaller.WriteInstallMarker(dstDir, entry);

            Assert.IsTrue(SampleArchiveInstaller.TryReadInstallMarker(dstDir, out InstalledSampleMarker marker));
            Assert.AreEqual("combat-stats", marker.sampleId);
            Assert.AreEqual("1.0.0", marker.version);

            Assert.IsTrue(SampleArchiveInstaller.RemoveInstalledSample(dstDir));
            Assert.IsFalse(Directory.Exists(dstDir));
        }

        [Test]
        public void SampleDownloadService_LocalCatalogCoversAllSevenPackagesAndEightSamples()
        {
            SampleManifest local = SampleDownloadService.TryLoadLocalCatalog();
            Assert.IsNotNull(local);
            Assert.IsNotNull(local.samples);
            Assert.AreEqual(8, local.samples.Length);

            Assert.AreEqual(1, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.core").Count);
            Assert.AreEqual(1, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.navigation").Count);
            Assert.AreEqual(1, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.stats").Count);
            Assert.AreEqual(2, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.dialog").Count);
            Assert.AreEqual(1, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.character").Count);
            Assert.AreEqual(1, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.camera").Count);
            Assert.AreEqual(1, SampleDownloadService.GetSamplesForPackage(local, "com.contentcontent.world").Count);
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
