using NUnit.Framework;
using SupermarketTycoon.Economy;

namespace SupermarketTycoon.Tests
{
    public sealed class WalletTests
    {
        [Test]
        public void TrySpend_WithEnoughMoney_Succeeds()
        {
            var wallet = new Wallet(150);

            var result = wallet.TrySpend(50);

            Assert.That(result, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(100));
        }

        [Test]
        public void TrySpend_WithoutEnoughMoney_FailsWithoutChangingBalance()
        {
            var wallet = new Wallet(25);

            var result = wallet.TrySpend(50);

            Assert.That(result, Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(25));
        }

        [Test]
        public void Wallet_CannotStartOrSpendWithNegativeValues()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new Wallet(-1));
            var wallet = new Wallet(10);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => wallet.TrySpend(-1));
            Assert.That(wallet.Balance, Is.EqualTo(10));
        }
    }
}
