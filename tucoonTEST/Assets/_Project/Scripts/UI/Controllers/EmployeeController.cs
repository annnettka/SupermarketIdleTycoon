using System;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Employees;
using SupermarketTycoon.Progression;

namespace SupermarketTycoon.UI
{
    /// <summary>
    /// Presents cashier progression and delegates validated purchases to EmployeeService.
    /// Отображает прогрессию кассира и передает проверенные покупки сервису EmployeeService.
    /// </summary>
    public sealed class EmployeeController : IDisposable
    {
        private readonly EmployeeView view;
        private readonly EmployeeService employees;
        private readonly IWallet wallet;
        private readonly ProgressionService progression;

        public EmployeeController(
            EmployeeView view,
            EmployeeService employees,
            IWallet wallet,
            ProgressionService progression)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.employees = employees ?? throw new ArgumentNullException(nameof(employees));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));

            employees.Changed += Refresh;
            wallet.BalanceChanged += OnBalanceChanged;
            progression.LevelChanged += OnLevelChanged;
            view.PurchaseButton.onClick.AddListener(OnPurchaseClicked);
            Refresh();
        }

        public void Dispose()
        {
            employees.Changed -= Refresh;
            wallet.BalanceChanged -= OnBalanceChanged;
            progression.LevelChanged -= OnLevelChanged;
            view.PurchaseButton.onClick.RemoveListener(OnPurchaseClicked);
        }

        private void OnPurchaseClicked()
        {
            employees.TryPurchaseNext();
            Refresh();
        }

        private void OnBalanceChanged(int _)
        {
            Refresh();
        }

        private void OnLevelChanged(int _)
        {
            Refresh();
        }

        private void Refresh()
        {
            var definition = employees.Definition;
            if (definition == null)
            {
                view.Show("CASHIER", "UNAVAILABLE", "UNAVAILABLE", false);
                return;
            }

            var result = employees.CanPurchaseNext();
            if (result == EmployeePurchaseResult.MaxLevel)
            {
                var current = definition.GetLevel(employees.CurrentLevel);
                view.Show("CASHIER", $"LEVEL {employees.CurrentLevel}   +{current.CheckoutSpeedBonus * 100f:0}% SPEED", "MAX LEVEL", false);
                return;
            }

            var next = definition.GetLevel(employees.CurrentLevel + 1);
            var status = employees.CurrentLevel == 0
                ? $"AVAILABLE AT LEVEL {next.RequiredPlayerLevel}"
                : $"LEVEL {employees.CurrentLevel}   CHECKOUT x{employees.SpeedMultiplier:0.00}";
            var button = result == EmployeePurchaseResult.Locked
                ? $"REQUIRES LEVEL {next.RequiredPlayerLevel}"
                : employees.CurrentLevel == 0 ? $"HIRE  ${next.Cost}" : $"UPGRADE  ${next.Cost}";
            view.Show("CASHIER", status, button, result == EmployeePurchaseResult.Success);
        }
    }
}
