using System;

namespace SupermarketTycoon.Economy
{
    public interface IWallet
    {
        int Balance { get; }
        event Action<int> BalanceChanged;
        void Add(int amount);
        bool CanSpend(int amount);
        bool TrySpend(int amount);
    }

    public sealed class Wallet : IWallet
    {
        public Wallet(int startingBalance)
        {
            if (startingBalance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingBalance));
            }

            Balance = startingBalance;
        }

        public int Balance { get; private set; }
        public event Action<int> BalanceChanged;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            checked
            {
                Balance += amount;
            }

            BalanceChanged?.Invoke(Balance);
        }

        public bool CanSpend(int amount)
        {
            return amount >= 0 && Balance >= amount;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (!CanSpend(amount))
            {
                return false;
            }

            Balance -= amount;
            BalanceChanged?.Invoke(Balance);
            return true;
        }
    }
}
