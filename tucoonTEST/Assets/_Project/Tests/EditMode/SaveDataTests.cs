using NUnit.Framework;
using SupermarketTycoon.Save;
using UnityEngine;

namespace SupermarketTycoon.Tests
{
    public sealed class SaveDataTests
    {
        [Test]
        public void JsonRoundTrip_RestoresCoreStateAndBuildingIds()
        {
            var source = new SaveData
            {
                Money = 215,
                CurrentLevel = 2,
                CurrentXp = 35
            };
            source.BuiltBuildings.Add(new BuiltBuildingData("spot.shelf.a", "shelf.basic"));

            var json = JsonUtility.ToJson(source);
            var restored = JsonUtility.FromJson<SaveData>(json);

            Assert.That(restored.Money, Is.EqualTo(215));
            Assert.That(restored.CurrentLevel, Is.EqualTo(2));
            Assert.That(restored.CurrentXp, Is.EqualTo(35));
            Assert.That(restored.BuiltBuildings, Has.Count.EqualTo(1));
            Assert.That(restored.BuiltBuildings[0].BuildSpotId, Is.EqualTo("spot.shelf.a"));
            Assert.That(restored.BuiltBuildings[0].BuildingDefinitionId, Is.EqualTo("shelf.basic"));
        }
    }
}
