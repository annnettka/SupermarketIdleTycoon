using NUnit.Framework;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Objectives;
using SupermarketTycoon.Offline;
using SupermarketTycoon.Progression;
using UnityEngine;

namespace SupermarketTycoon.Tests
{
    public sealed class FeatureExpansionTests
    {
        [Test]
        public void Progression_StopsAtConfiguredLevelFive()
        {
            var config = ScriptableObject.CreateInstance<ProgressionConfig>();
            var progression = new ProgressionService(config, 1, 0);

            progression.AddXp(5000);

            Assert.That(progression.CurrentLevel, Is.EqualTo(5));
            Assert.That(progression.CurrentXp, Is.EqualTo(0));
            Assert.That(progression.XpToNextLevel, Is.EqualTo(0));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void StoreRating_IsBoundedAndChangesTrafficAtThresholds()
        {
            var rating = new StoreRatingService(3f);
            for (var i = 0; i < 30; i++)
            {
                rating.RecordCustomerLost();
            }

            Assert.That(rating.CurrentRating, Is.EqualTo(1f));
            Assert.That(rating.TrafficIntervalMultiplier, Is.EqualTo(1.15f));

            for (var i = 0; i < 150; i++)
            {
                rating.RecordSuccessfulTransaction();
            }

            Assert.That(rating.CurrentRating, Is.EqualTo(5f));
            Assert.That(rating.TrafficIntervalMultiplier, Is.EqualTo(0.9f));
        }

        [Test]
        public void ObjectiveCompletion_AwardsMoneyAndAdvances()
        {
            var progressionConfig = ScriptableObject.CreateInstance<ProgressionConfig>();
            var objectiveConfig = ScriptableObject.CreateInstance<ObjectiveConfig>();
            objectiveConfig.Configure(new[]
            {
                new ObjectiveDefinition(ObjectiveType.ServeCustomers, "Serve one", 1, null, 25, 10),
                new ObjectiveDefinition(ObjectiveType.EarnMoney, "Earn money", 50)
            });
            var wallet = new Wallet(0);
            var economy = new EconomyService(wallet, 20);
            var progression = new ProgressionService(progressionConfig, 1, 0);
            var objectives = new ObjectiveService(objectiveConfig, economy, progression, 0, 0);

            objectives.RecordCustomerServed();

            Assert.That(objectives.CurrentIndex, Is.EqualTo(1));
            Assert.That(wallet.Balance, Is.EqualTo(25));
            Assert.That(progression.CurrentXp, Is.EqualTo(10));
            Object.DestroyImmediate(objectiveConfig);
            Object.DestroyImmediate(progressionConfig);
        }

        [Test]
        public void Economy_AppliesConfiguredCustomerMultiplier()
        {
            var wallet = new Wallet(0);
            var economy = new EconomyService(wallet, 20);

            var paid = economy.AddCustomerIncome(1.5f);

            Assert.That(paid, Is.EqualTo(30));
            Assert.That(wallet.Balance, Is.EqualTo(30));
        }

        [Test]
        public void OfflineIncome_CollectsPendingAmountExactlyOnce()
        {
            var wallet = new Wallet(0);
            var economy = new EconomyService(wallet, 20);
            var offline = new OfflineIncomeService(null, null, economy, 0, 125);

            var first = offline.Collect();
            var second = offline.Collect();

            Assert.That(first, Is.EqualTo(125));
            Assert.That(second, Is.EqualTo(0));
            Assert.That(wallet.Balance, Is.EqualTo(125));
            Assert.That(offline.PendingIncome, Is.EqualTo(0));
        }
    }
}
