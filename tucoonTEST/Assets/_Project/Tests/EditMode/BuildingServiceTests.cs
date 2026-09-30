using System.Collections.Generic;
using NUnit.Framework;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Progression;
using UnityEditor;
using UnityEngine;

namespace SupermarketTycoon.Tests
{
    public sealed class BuildingServiceTests
    {
        private readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = cleanup.Count - 1; i >= 0; i--)
            {
                if (cleanup[i] != null)
                {
                    Object.DestroyImmediate(cleanup[i]);
                }
            }

            cleanup.Clear();
        }

        [Test]
        public void TryBuild_ValidatesLevelFundsAndSpendsOnlyOnSuccess()
        {
            var prefab = Track(new GameObject("Test Shelf Prefab"));
            var definition = CreateDefinition(
                prefab,
                50,
                2,
                new BuildingLevelDefinition(1, 0, 2, 1, 1f, 2f, 1f));
            var spot = CreateSpot(definition);
            var wallet = new Wallet(40);
            var progressionConfig = Track(ScriptableObject.CreateInstance<ProgressionConfig>());
            var progression = new ProgressionService(progressionConfig, 1, 0);
            var service = CreateService(wallet, progression);

            Assert.That(service.TryBuild(spot), Is.EqualTo(PurchaseResult.Locked));
            Assert.That(wallet.Balance, Is.EqualTo(40));

            progression.AddXp(100);
            Assert.That(service.TryBuild(spot), Is.EqualTo(PurchaseResult.InsufficientFunds));
            Assert.That(wallet.Balance, Is.EqualTo(40));

            wallet.Add(60);
            Assert.That(service.TryBuild(spot), Is.EqualTo(PurchaseResult.Success));
            Assert.That(wallet.Balance, Is.EqualTo(50));
            Assert.That(spot.IsBuilt, Is.True);
            Assert.That(service.TryBuild(spot), Is.EqualTo(PurchaseResult.AlreadyBuilt));
            Assert.That(wallet.Balance, Is.EqualTo(50));

            service.Dispose();
        }

        [Test]
        public void TryUpgrade_SpendsConfiguredCostAndRejectsMaxLevel()
        {
            var prefab = Track(new GameObject("Test Shelf Prefab"));
            var definition = CreateDefinition(
                prefab,
                50,
                1,
                new BuildingLevelDefinition(1, 0, 1, 1, 1f, 2f, 1f),
                new BuildingLevelDefinition(2, 100, 1, 2, 1.25f, 1.5f, 1.1f));
            var spot = CreateSpot(definition);
            var building = new GameObject("Built Shelf");
            building.transform.SetParent(spot.transform, false);
            spot.MarkBuilt(building, 1);

            var wallet = new Wallet(150);
            var progressionConfig = Track(ScriptableObject.CreateInstance<ProgressionConfig>());
            var progression = new ProgressionService(progressionConfig, 1, 0);
            var buildings = CreateService(wallet, progression);
            var upgrades = new BuildingUpgradeService(wallet, progression, buildings, CreateSilentAudio());

            Assert.That(upgrades.TryUpgrade(spot), Is.EqualTo(UpgradeResult.Success));
            Assert.That(spot.CurrentLevel, Is.EqualTo(2));
            Assert.That(wallet.Balance, Is.EqualTo(50));
            Assert.That(upgrades.TryUpgrade(spot), Is.EqualTo(UpgradeResult.MaxLevel));
            Assert.That(wallet.Balance, Is.EqualTo(50));

            buildings.Dispose();
        }

        private BuildingService CreateService(Wallet wallet, ProgressionService progression)
        {
            return new BuildingService(wallet, progression, new StationRegistry(), CreateSilentAudio());
        }

        private BuildSpot CreateSpot(BuildingDefinition definition)
        {
            var root = Track(new GameObject("Build Spot"));
            var spot = root.AddComponent<BuildSpot>();
            spot.Configure("spot.test", definition, root.transform, null);
            spot.SetExpansionUnlocked(true);
            return spot;
        }

        private BuildingDefinition CreateDefinition(
            GameObject prefab,
            int cost,
            int requiredLevel,
            params BuildingLevelDefinition[] levels)
        {
            var definition = Track(ScriptableObject.CreateInstance<BuildingDefinition>());
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = "building.test";
            serialized.FindProperty("displayName").stringValue = "Test Building";
            serialized.FindProperty("type").enumValueIndex = (int)BuildingType.Shelf;
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.FindProperty("cost").intValue = cost;
            serialized.FindProperty("requiredLevel").intValue = requiredLevel;
            var upgrades = serialized.FindProperty("upgradeLevels");
            upgrades.arraySize = levels.Length;
            for (var i = 0; i < levels.Length; i++)
            {
                var level = upgrades.GetArrayElementAtIndex(i);
                level.FindPropertyRelative("level").intValue = levels[i].Level;
                level.FindPropertyRelative("upgradeCost").intValue = levels[i].UpgradeCost;
                level.FindPropertyRelative("requiredPlayerLevel").intValue = levels[i].RequiredPlayerLevel;
                level.FindPropertyRelative("capacity").intValue = levels[i].Capacity;
                level.FindPropertyRelative("incomeMultiplier").floatValue = levels[i].IncomeMultiplier;
                level.FindPropertyRelative("interactionDuration").floatValue = levels[i].InteractionDuration;
                level.FindPropertyRelative("visualScale").floatValue = levels[i].VisualScale;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static AudioService CreateSilentAudio()
        {
            return new AudioService(null, null, null, null, null, null);
        }

        private T Track<T>(T value) where T : Object
        {
            cleanup.Add(value);
            return value;
        }
    }
}
