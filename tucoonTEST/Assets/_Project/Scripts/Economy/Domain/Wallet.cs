using System;

namespace SupermarketTycoon.Economy
{
    /// <summary>
    /// Defines the minimal balance and spending contract required by economy use cases.
    /// Определяет минимальный контракт баланса и расходов, необходимый экономическим сценариям.
    /// </summary>
    public interface IWallet
    {
        int Balance { get; }
        event Action<int> BalanceChanged;

        /// <summary>
        /// Adds a strictly positive amount and publishes the resulting balance.
        /// Добавляет строго положительную сумму и публикует итоговый баланс.
        /// </summary>
        void Add(int amount);

        /// <summary>
        /// Checks affordability without mutating the balance.
        /// Проверяет возможность расхода без изменения баланса.
        /// </summary>
        bool CanSpend(int amount);

        /// <summary>
        /// Atomically spends a non-negative amount only when sufficient funds exist.
        /// Атомарно списывает неотрицательную сумму только при наличии достаточных средств.
        /// </summary>
        bool TrySpend(int amount);
    }

    /// <summary>
    /// Owns authoritative money state and guarantees that balance and spending never become negative.
    /// Владеет авторитетным состоянием денег и гарантирует, что баланс и расходы не становятся отрицательными.
    /// </summary>
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
