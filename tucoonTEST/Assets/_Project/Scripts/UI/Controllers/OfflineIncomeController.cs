using System;
using SupermarketTycoon.Offline;

namespace SupermarketTycoon.UI
{
    /// <summary>
    /// Shows pending offline income and commits collection through OfflineIncomeService.
    /// Показывает ожидающий офлайн-доход и выполняет его получение через OfflineIncomeService.
    /// </summary>
    public sealed class OfflineIncomeController : IDisposable
    {
        private readonly OfflineIncomeView view;
        private readonly OfflineIncomeService offline;

        public OfflineIncomeController(OfflineIncomeView view, OfflineIncomeService offline)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.offline = offline ?? throw new ArgumentNullException(nameof(offline));
            view.CollectButton.onClick.AddListener(Collect);
            view.Show(offline.PendingIncome);
        }

        public void Dispose()
        {
            view.CollectButton.onClick.RemoveListener(Collect);
        }

        private void Collect()
        {
            offline.Collect();
            view.Hide();
        }
    }
}
