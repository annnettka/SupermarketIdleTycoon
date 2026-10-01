using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    /// <summary>
    /// Plays a self-contained world-to-screen income notification and returns it to its inactive state.
    /// Воспроизводит автономное уведомление дохода из мира на экран и возвращает его в неактивное состояние.
    /// </summary>
    public sealed class FloatingIncomeView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        public void Configure(Text text, CanvasGroup group, RectTransform rect)
        {
            label = text;
            canvasGroup = group;
            rectTransform = rect;
        }

        public void Play(int amount)
        {
            label.text = $"+${amount}";
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            var elapsed = 0f;
            var start = rectTransform.anchoredPosition;
            const float duration = 1.1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                rectTransform.anchoredPosition = start + Vector2.up * (70f * t);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
