using System;
using SupermarketTycoon.Save;

namespace SupermarketTycoon.Stats
{
    public sealed class StatisticsService
    {
        private readonly LifetimeStatsData data;

        public StatisticsService(LifetimeStatsData loadedData)
        {
            data = loadedData?.Clone() ?? new LifetimeStatsData();
        }

        public int CustomersServed => data.CustomersServed;
        public int CustomersLost => data.CustomersLost;
        public long TotalMoneyEarned => data.TotalMoneyEarned;
        public int TotalBuildingsPurchased => data.TotalBuildingsPurchased;
        public int TotalUpgradesPurchased => data.TotalUpgradesPurchased;

        public event Action Changed;

        public void RecordCustomerServed()
        {
            data.CustomersServed++;
            Changed?.Invoke();
        }

        public void RecordCustomerLost()
        {
            data.CustomersLost++;
            Changed?.Invoke();
        }

        public void RecordIncome(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            data.TotalMoneyEarned += amount;
            Changed?.Invoke();
        }

        public void RecordBuildingPurchased()
        {
            data.TotalBuildingsPurchased++;
            Changed?.Invoke();
        }

        public void RecordUpgradePurchased()
        {
            data.TotalUpgradesPurchased++;
            Changed?.Invoke();
        }

        public LifetimeStatsData Capture()
        {
            return data.Clone();
        }
    }
}
