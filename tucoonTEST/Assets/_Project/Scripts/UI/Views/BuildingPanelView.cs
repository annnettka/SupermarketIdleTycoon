using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    public sealed class BuildingPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text primaryStatLabel;
        [SerializeField] private Text secondaryStatLabel;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Text upgradeButtonLabel;
        [SerializeField] private Button closeButton;

        public Button UpgradeButton => upgradeButton;
        public Button CloseButton => closeButton;

        public void Configure(
            GameObject panelRoot,
            Text title,
            Text level,
            Text primaryStat,
            Text secondaryStat,
            Button upgrade,
            Text upgradeLabel,
            Button close)
        {
            root = panelRoot;
            titleLabel = title;
            levelLabel = level;
            primaryStatLabel = primaryStat;
            secondaryStatLabel = secondaryStat;
            upgradeButton = upgrade;
            upgradeButtonLabel = upgradeLabel;
            closeButton = close;
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }

        public void Show(
            string title,
            int level,
            string primaryStat,
            string secondaryStat,
            string buttonText,
            bool canUpgrade)
        {
            titleLabel.text = title.ToUpperInvariant();
            levelLabel.text = $"LEVEL {level}";
            primaryStatLabel.text = primaryStat;
            secondaryStatLabel.text = secondaryStat;
            upgradeButtonLabel.text = buttonText;
            upgradeButton.interactable = canUpgrade;
        }
    }
}
