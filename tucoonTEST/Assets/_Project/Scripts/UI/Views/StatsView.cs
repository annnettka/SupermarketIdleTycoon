using SupermarketTycoon.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    /// <summary>
    /// Renders a read-only lifetime statistics snapshot supplied by the pause controller.
    /// Отображает переданный контроллером паузы неизменяемый снимок общей статистики.
    /// </summary>
    public sealed class StatsView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text statsLabel;
        [SerializeField] private Button backButton;

        public Button BackButton => backButton;

        public void Configure(GameObject panelRoot, Text stats, Button back)
        {
            root = panelRoot;
            statsLabel = stats;
            backButton = back;
        }

        public void Show(StatisticsService stats)
        {
            statsLabel.text =
                $"CUSTOMERS SERVED     {stats.CustomersServed}\n" +
                $"CUSTOMERS LOST       {stats.CustomersLost}\n" +
                $"TOTAL MONEY EARNED   ${stats.TotalMoneyEarned}\n" +
                $"BUILDINGS PURCHASED  {stats.TotalBuildingsPurchased}\n" +
                $"UPGRADES PURCHASED   {stats.TotalUpgradesPurchased}";
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}
