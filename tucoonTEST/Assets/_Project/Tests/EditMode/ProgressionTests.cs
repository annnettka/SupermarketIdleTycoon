using NUnit.Framework;
using SupermarketTycoon.Progression;
using UnityEngine;

namespace SupermarketTycoon.Tests
{
    public sealed class ProgressionTests
    {
        [Test]
        public void AddXp_LevelsAndCarriesOverflow()
        {
            var config = ScriptableObject.CreateInstance<ProgressionConfig>();
            var progression = new ProgressionService(config, 1, 90);

            progression.AddXp(20);

            Assert.That(progression.CurrentLevel, Is.EqualTo(2));
            Assert.That(progression.CurrentXp, Is.EqualTo(10));
            Object.DestroyImmediate(config);
        }
    }
}
