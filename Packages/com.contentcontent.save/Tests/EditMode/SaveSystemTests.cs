using System;
using System.IO;
using NUnit.Framework;

namespace ContentContent.Save.Tests
{
    public class SaveSystemTests
    {
        private sealed class FakePlayerSaveable : ISaveable
        {
            public string SaveId => "player";
            public int Level = 1;
            public long Xp = 0;
            public float Health = 100f;
            public double PrecisionTimer = 0.0;
            public bool HasKey;
            public string Name = "Hero";

            public void CaptureState(SaveSection section)
            {
                section.SetInt("level", Level);
                section.SetLong("xp", Xp);
                section.SetFloat("health", Health);
                section.SetDouble("timer", PrecisionTimer);
                section.SetBool("has_key", HasKey);
                section.SetString("name", Name);
            }

            public void RestoreState(SaveSection section)
            {
                Level = section.GetInt("level", 1);
                Xp = section.GetLong("xp", 0L);
                Health = section.GetFloat("health", 100f);
                PrecisionTimer = section.GetDouble("timer", 0.0);
                HasKey = section.GetBool("has_key", false);
                Name = section.GetString("name", "Hero");
            }
        }

        private sealed class UpgradeV1ToV2Migration : ISaveMigration
        {
            public int FromVersion => 1;
            public int ToVersion => 2;

            public void Migrate(SaveDocument document)
            {
                if (document.TryGetSection("player", out SaveSection player) && player.Has("hp"))
                {
                    player.SetFloat("health", player.GetFloat("hp"));
                    player.Remove("hp");
                }
            }
        }

        private sealed class UpgradeV2ToV3Migration : ISaveMigration
        {
            public int FromVersion => 2;
            public int ToVersion => 3;

            public void Migrate(SaveDocument document)
            {
                SaveSection player = document.GetOrCreateSection("player");
                if (!player.Has("xp"))
                {
                    player.SetLong("xp", player.GetInt("level", 1) * 100L);
                }
            }
        }

        [Test]
        public void SaveJsonCodec_RoundTripsAllValueTypesAndEscapedStrings()
        {
            var doc = new SaveDocument(schemaVersion: 2);
            doc.Metadata = new SaveMetadata
            {
                SlotId = "slot_a",
                DisplayName = "Slot \"Alpha\"\nLine2",
                SchemaVersion = 2,
                SavedAtUtcTicks = 123456789012345L,
                PlayTimeSeconds = 42.5f,
                LocationLabel = "North\\Gate"
            };

            SaveSection sec = doc.GetOrCreateSection("inventory");
            sec.SetBool("flag", true);
            sec.SetInt("gold", -250);
            sec.SetLong("score", 9000000000123L);
            sec.SetFloat("stamina", 75.25f);
            sec.SetDouble("precise", 3.14159265358979);
            sec.SetString("note", "Line1\nLine2\t\"Quoted\"\\Slash");

            string json = SaveJsonCodec.Encode(doc);
            SaveDocument decoded = SaveJsonCodec.Decode(json);

            Assert.AreEqual(2, decoded.SchemaVersion);
            Assert.AreEqual("slot_a", decoded.Metadata.SlotId);
            Assert.AreEqual("Slot \"Alpha\"\nLine2", decoded.Metadata.DisplayName);
            Assert.AreEqual(123456789012345L, decoded.Metadata.SavedAtUtcTicks);
            Assert.AreEqual(42.5f, decoded.Metadata.PlayTimeSeconds, 0.001f);
            Assert.AreEqual("North\\Gate", decoded.Metadata.LocationLabel);

            Assert.IsTrue(decoded.TryGetSection("inventory", out SaveSection outSec));
            Assert.IsTrue(outSec.GetBool("flag"));
            Assert.AreEqual(-250, outSec.GetInt("gold"));
            Assert.AreEqual(9000000000123L, outSec.GetLong("score"));
            Assert.AreEqual(75.25f, outSec.GetFloat("stamina"), 0.0001f);
            Assert.AreEqual(3.14159265358979, outSec.GetDouble("precise"), 1e-12);
            Assert.AreEqual("Line1\nLine2\t\"Quoted\"\\Slash", outSec.GetString("note"));
        }

        [Test]
        public void SaveManager_SaveAndLoad_RestoresParticipantsAndAppliesMigrationChain()
        {
            var storage = new InMemorySaveStorage();

            // Write legacy v1 document manually
            var v1Doc = new SaveDocument(schemaVersion: 1);
            v1Doc.Metadata = new SaveMetadata { SlotId = "legacy", SchemaVersion = 1 };
            SaveSection pSec = v1Doc.GetOrCreateSection("player");
            pSec.SetInt("level", 4);
            pSec.SetFloat("hp", 62.5f);
            pSec.SetBool("has_key", true);
            pSec.SetString("name", "Aria");
            storage.WriteAtomic("legacy", SaveJsonCodec.Encode(v1Doc));

            var manager = new SaveManager(storage, currentSchemaVersion: 3);
            var player = new FakePlayerSaveable();
            manager.Register(player);
            manager.RegisterMigration(new UpgradeV1ToV2Migration());
            manager.RegisterMigration(new UpgradeV2ToV3Migration());

            int migrationEvents = 0;
            manager.OnMigrationApplied += (_, _) => migrationEvents++;

            bool loaded = manager.TryLoadSlot("legacy", out SaveDocument loadedDoc);

            Assert.IsTrue(loaded);
            Assert.AreEqual(2, migrationEvents);
            Assert.AreEqual(3, loadedDoc.SchemaVersion);
            Assert.AreEqual(4, player.Level);
            Assert.AreEqual(62.5f, player.Health, 0.001f);
            Assert.AreEqual(400L, player.Xp);
            Assert.IsTrue(player.HasKey);
            Assert.AreEqual("Aria", player.Name);
        }

        [Test]
        public void FileSaveStorage_AtomicBackupFallbackAndTraversalRejection()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "cc_save_tests_" + Guid.NewGuid().ToString("N"));
            try
            {
                var storage = new FileSaveStorage(tempDir);
                Assert.Throws<ArgumentException>(() => SaveSlotUtility.ValidateSlotId("../escape"));
                Assert.Throws<ArgumentException>(() => storage.WriteAtomic("bad/slot", "{}"));

                var manager = new SaveManager(storage, currentSchemaVersion: 1);
                var player = new FakePlayerSaveable { Level = 7, Name = "Initial" };
                manager.Register(player);
                manager.SaveSlot("slot_0", locationLabel: "Hub");

                // Second write rotates old slot_0.json to slot_0.json.bak
                player.Level = 12;
                player.Name = "Updated";
                manager.SaveSlot("slot_0", locationLabel: "Citadel");

                // Corrupt primary file on disk to simulate mid-crash truncation
                File.WriteAllText(storage.GetSlotFilePath("slot_0"), "{ corrupted json");

                player.Level = 1;
                player.Name = "Wiped";
                bool loaded = manager.TryLoadSlot("slot_0", out SaveDocument recovered);

                Assert.IsTrue(loaded);
                Assert.AreEqual(7, player.Level);
                Assert.AreEqual("Initial", player.Name);
                Assert.AreEqual("Hub", recovered.Metadata.LocationLabel);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
    }
}
