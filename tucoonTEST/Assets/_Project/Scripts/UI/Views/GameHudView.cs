using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    /// <summary>
    /// Renders session status and exposes HUD commands without mutating gameplay services directly.
    /// Отображает состояние сессии и предоставляет команды HUD, не изменяя игровые сервисы напрямую.
    /// </summary>
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField] private Text moneyLabel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Slider xpBar;
        [SerializeField] private Text xpLabel;
        [SerializeField] private Text customerLabel;
        [SerializeField] private Text ratingLabel;
        [SerializeField] private Text objectiveLabel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private FloatingIncomeView floatingIncomePrefab;
        [SerializeField] private RectTransform floatingIncomeRoot;
        [SerializeField] private GameObject notificationRoot;
        [SerializeField] private CanvasGroup notificationGroup;
        [SerializeField] private Text notificationLabel;

        private Coroutine notificationRoutine;

        public Button PauseButton => pauseButton;

        public void Configure(
            Text money,
            Text level,
            Slider xp,
            Text xpText,
            Text customers,
            Text rating,
            Text objective,
            Button pause,
            FloatingIncomeView floatingPrefab,
            RectTransform floatingRoot,
            GameObject notification,
            CanvasGroup notificationCanvasGroup,
            Text notificationText)
        {
            moneyLabel = money;
            levelLabel = level;
            xpBar = xp;
            xpLabel = xpText;
            customerLabel = customers;
            ratingLabel = rating;
            objectiveLabel = objective;
            pauseButton = pause;
            floatingIncomePrefab = floatingPrefab;
            floatingIncomeRoot = floatingRoot;
            notificationRoot = notification;
            notificationGroup = notificationCanvasGroup;
            notificationLabel = notificationText;
        }

        public void SetMoney(int money)
        {
            moneyLabel.text = "$" + money.ToString("N0", CultureInfo.InvariantCulture);
        }

        public void SetProgression(int level, int xp, int requiredXp)
        {
            levelLabel.text = $"LEVEL {level}";
            var isMaximum = requiredXp <= 0;
            xpBar.SetValueWithoutNotify(isMaximum ? 1f : (float)xp / requiredXp);
            xpLabel.text = isMaximum ? "STORE ESTABLISHED" : $"{xp} / {requiredXp} XP";
        }

        public void SetCustomers(int count)
        {
            customerLabel.text = $"CUSTOMERS\n{count}";
        }

        public void SetRating(float rating)
        {
            ratingLabel.text = $"RATING\n{rating:0.0} / 5";
        }

        public void SetObjective(string title, string progress, bool sequenceComplete)
        {
            objectiveLabel.text = sequenceComplete
                ? "CURRENT GOAL  |  STORE ESTABLISHED"
                : $"CURRENT GOAL  |  {title.ToUpperInvariant()}  {progress}";
        }

        public void ShowIncome(int amount)
        {
            if (floatingIncomePrefab != null && floatingIncomeRoot != null)
            {
                var view = Instantiate(floatingIncomePrefab, floatingIncomeRoot);
                view.Play(amount);
            }
        }

        public void ShowNotification(string title, string detail)
        {
            if (notificationRoot == null || notificationGroup == null || notificationLabel == null)
            {
                return;
            }

            if (notificationRoutine != null)
            {
                StopCoroutine(notificationRoutine);
            }

            notificationRoutine = StartCoroutine(PlayNotification(title, detail));
        }

        private IEnumerator PlayNotification(string title, string detail)
        {
            notificationRoot.SetActive(true);
            notificationLabel.text = string.IsNullOrEmpty(detail) ? title : $"{title}\n{detail}";
            notificationGroup.alpha = 0f;
            var elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.unscaledDeltaTime;
                notificationGroup.alpha = Mathf.Clamp01(elapsed / 0.18f);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(1.8f);
            elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.unscaledDeltaTime;
                notificationGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.25f);
                yield return null;
            }

            notificationRoot.SetActive(false);
            notificationRoutine = null;
        }
    }
}
