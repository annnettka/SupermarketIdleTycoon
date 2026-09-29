using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField] private Text moneyLabel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Slider xpBar;
        [SerializeField] private Text xpLabel;
        [SerializeField] private Text customerLabel;
        [SerializeField] private Text objectiveLabel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private FloatingIncomeView floatingIncomePrefab;
        [SerializeField] private RectTransform floatingIncomeRoot;

        public Button PauseButton => pauseButton;

        public void Configure(
            Text money,
            Text level,
            Slider xp,
            Text xpText,
            Text customers,
            Text objective,
            Button pause,
            FloatingIncomeView floatingPrefab,
            RectTransform floatingRoot)
        {
            moneyLabel = money;
            levelLabel = level;
            xpBar = xp;
            xpLabel = xpText;
            customerLabel = customers;
            objectiveLabel = objective;
            pauseButton = pause;
            floatingIncomePrefab = floatingPrefab;
            floatingIncomeRoot = floatingRoot;
        }

        public void SetMoney(int money)
        {
            moneyLabel.text = $"${money}";
        }

        public void SetProgression(int level, int xp, int requiredXp)
        {
            levelLabel.text = $"LEVEL {level}";
            xpBar.SetValueWithoutNotify(requiredXp > 0 ? (float)xp / requiredXp : 0f);
            xpLabel.text = $"{xp} / {requiredXp} XP";
        }

        public void SetCustomers(int count)
        {
            customerLabel.text = $"CUSTOMERS  {count}";
        }

        public void SetObjective(bool operational)
        {
            objectiveLabel.text = operational
                ? "Customers are shopping automatically"
                : "Build a shelf and a checkout";
        }

        public void ShowIncome(int amount)
        {
            if (floatingIncomePrefab != null && floatingIncomeRoot != null)
            {
                var view = Instantiate(floatingIncomePrefab, floatingIncomeRoot);
                view.Play(amount);
            }
        }
    }
}
