using System;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Employees;
using UnityEngine;

namespace SupermarketTycoon.Offline
{
    /// <summary>
    /// Estimates capped offline earnings from built capacity and automation, then transfers them exactly once.
    /// Оценивает ограниченный офлайн-доход по построенной мощности и автоматизации, затем начисляет его ровно один раз.
    /// </summary>
    public sealed class OfflineIncomeService
    {
        private const double MaximumOfflineMinutes = 120d;
        private const float OfflineEfficiency = 0.35f;

        private readonly EconomyService economy;

        public OfflineIncomeService(
            BuildingService buildings,
            EmployeeService employees,
            EconomyService economy,
            long lastSaveUtcTicks,
            int pendingIncome)
        {
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
            PendingIncome = Math.Max(0, pendingIncome);

            if (buildings == null || lastSaveUtcTicks <= 0)
            {
                return;
            }

            var elapsed = DateTime.UtcNow - new DateTime(lastSaveUtcTicks, DateTimeKind.Utc);
            var minutesAway = Math.Min(MaximumOfflineMinutes, Math.Max(0d, elapsed.TotalMinutes));
            if (minutesAway < 1d)
            {
                return;
            }

            buildings.GetOfflineMetrics(out var shelfCapacity, out var checkoutCapacity, out var incomeMultiplier);
            if (shelfCapacity <= 0 || checkoutCapacity <= 0)
            {
                return;
            }

            var estimatedCustomersPerMinute = Mathf.Min(shelfCapacity * 3f, checkoutCapacity * 2.5f);
            var employeeMultiplier = employees != null ? employees.SpeedMultiplier : 1f;
            var incomePerMinute = estimatedCustomersPerMinute * economy.CustomerPayment * incomeMultiplier * employeeMultiplier;
            PendingIncome += Mathf.Max(0, Mathf.FloorToInt((float)minutesAway * incomePerMinute * OfflineEfficiency));
        }

        public int PendingIncome { get; private set; }
        public float Efficiency => OfflineEfficiency;
        public int MaximumMinutes => (int)MaximumOfflineMinutes;

        public event Action Changed;

        public int Collect()
        {
            if (PendingIncome <= 0)
            {
                return 0;
            }

            var collected = PendingIncome;
            PendingIncome = 0;
            economy.AddIncome(collected);
            Changed?.Invoke();
            return collected;
        }

        public long CaptureCurrentUtcTicks()
        {
            return DateTime.UtcNow.Ticks;
        }
    }
}
