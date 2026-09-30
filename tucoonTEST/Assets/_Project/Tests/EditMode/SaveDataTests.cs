using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SupermarketTycoon.Core;
using SupermarketTycoon.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace SupermarketTycoon.Tests
{
    public sealed class SaveDataTests
    {
        [Test]
        public void JsonRoundTrip_RestoresExpandedProgressState()
        {
            var source = new SaveData
            {
                Money = 215,
                CurrentLevel = 2,
                CurrentXp = 35,
                CashierLevel = 1,
                StoreRating = 3.7f,
                CurrentObjectiveIndex = 4,
                CurrentObjectiveProgress = 2,
                LastSaveUtcTicks = 123456,
                PendingOfflineIncome = 80
            };
            source.BuiltBuildings.Add(new BuiltBuildingData("spot.shelf.a", "shelf.basic", 2));
            source.PurchasedExpansionIds.Add("expansion.main");
            source.LifetimeStats.CustomersServed = 12;

            var json = JsonUtility.ToJson(source);
            var restored = JsonUtility.FromJson<SaveData>(json);

            Assert.That(restored.Money, Is.EqualTo(215));
            Assert.That(restored.CurrentLevel, Is.EqualTo(2));
            Assert.That(restored.CurrentXp, Is.EqualTo(35));
            Assert.That(restored.BuiltBuildings, Has.Count.EqualTo(1));
            Assert.That(restored.BuiltBuildings[0].BuildingLevel, Is.EqualTo(2));
            Assert.That(restored.PurchasedExpansionIds, Does.Contain("expansion.main"));
            Assert.That(restored.CashierLevel, Is.EqualTo(1));
            Assert.That(restored.StoreRating, Is.EqualTo(3.7f).Within(0.001f));
            Assert.That(restored.LifetimeStats.CustomersServed, Is.EqualTo(12));
            Assert.That(restored.PendingOfflineIncome, Is.EqualTo(80));
        }

        [Test]
        public void LoadOrCreate_MigratesVersionOneBuildingToLevelOne()
        {
            var directory = Path.Combine(Path.GetTempPath(), "supermarket-save-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var config = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                File.WriteAllText(
                    Path.Combine(directory, "supermarket-save.json"),
                    "{\"SaveVersion\":1,\"Money\":90,\"CurrentLevel\":2,\"CurrentXp\":10," +
                    "\"BuiltBuildings\":[{\"BuildSpotId\":\"spot.shelf.a\",\"BuildingDefinitionId\":\"shelf.basic\"}]}");

                var repository = new JsonSaveRepository(directory);
                var loaded = repository.LoadOrCreate(config);

                Assert.That(loaded.SaveVersion, Is.EqualTo(SaveData.CurrentVersion));
                Assert.That(loaded.Money, Is.EqualTo(90));
                Assert.That(loaded.BuiltBuildings[0].BuildingLevel, Is.EqualTo(1));
                Assert.That(loaded.StoreRating, Is.EqualTo(3f));
                Assert.That(loaded.LifetimeStats, Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void LoadOrCreate_CorruptedSaveReturnsFreshDataAndKeepsBackup()
        {
            var directory = Path.Combine(Path.GetTempPath(), "supermarket-corrupt-save-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var config = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                File.WriteAllText(Path.Combine(directory, "supermarket-save.json"), "{ definitely not valid json");
                LogAssert.Expect(LogType.Warning, new Regex("Could not load gameplay save.*"));

                var repository = new JsonSaveRepository(directory);
                var loaded = repository.LoadOrCreate(config);

                Assert.That(loaded.SaveVersion, Is.EqualTo(SaveData.CurrentVersion));
                Assert.That(loaded.Money, Is.EqualTo(config.StartingMoney));
                Assert.That(File.Exists(Path.Combine(directory, "supermarket-save.backup.json")), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void Delete_RemovesGameplaySaveForResetProgress()
        {
            var directory = Path.Combine(Path.GetTempPath(), "supermarket-reset-save-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var repository = new JsonSaveRepository(directory);
                repository.Save(new SaveData { Money = 123 });
                Assert.That(repository.HasSave, Is.True);

                repository.Delete();

                Assert.That(repository.HasSave, Is.False);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
