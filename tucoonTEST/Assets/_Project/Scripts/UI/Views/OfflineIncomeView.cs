using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    /// <summary>
    /// Presents one pending offline reward and exposes its collection command.
    /// Отображает одну ожидающую офлайн-награду и предоставляет команду ее получения.
    /// </summary>
    public sealed class OfflineIncomeView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text amountLabel;
        [SerializeField] private Button collectButton;

        public Button CollectButton => collectButton;

        public void Configure(GameObject overlayRoot, Text amount, Button collect)
        {
            root = overlayRoot;
            amountLabel = amount;
            collectButton = collect;
        }

        public void Show(int amount)
        {
            root.SetActive(amount > 0);
            amountLabel.text = $"WHILE YOU WERE AWAY\n+${amount}";
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}
