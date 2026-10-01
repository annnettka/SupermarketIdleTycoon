using System;
using UnityEngine;

namespace SupermarketTycoon.Economy
{
    /// <summary>
    /// Implements income use cases over an injected wallet and publishes accepted earnings to observers.
    /// Реализует сценарии начисления дохода поверх внедренного кошелька и сообщает наблюдателям о принятых начислениях.
    /// </summary>
    public sealed class EconomyService
    {
        private readonly IWallet wallet;

        public EconomyService(IWallet wallet, int customerPayment)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            CustomerPayment = Math.Max(1, customerPayment);
        }

        public int CustomerPayment { get; }
        public event Action<int> IncomeAdded;

        public int AddCustomerIncome(float multiplier = 1f)
        {
            var amount = Mathf.Max(1, Mathf.RoundToInt(CustomerPayment * Mathf.Max(0.01f, multiplier)));
            AddIncome(amount);
            return amount;
        }

        public void AddIncome(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            wallet.Add(amount);
            IncomeAdded?.Invoke(amount);
        }
    }
}
