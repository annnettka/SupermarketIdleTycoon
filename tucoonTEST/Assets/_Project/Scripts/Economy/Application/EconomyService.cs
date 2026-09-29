using System;

namespace SupermarketTycoon.Economy
{
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

        public void AddCustomerIncome()
        {
            wallet.Add(CustomerPayment);
            IncomeAdded?.Invoke(CustomerPayment);
        }
    }
}
