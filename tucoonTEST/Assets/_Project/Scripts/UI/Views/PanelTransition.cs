using System.Collections;
using UnityEngine;

namespace SupermarketTycoon.UI
{
    /// <summary>
    /// Owns reusable unscaled fade and slide transitions while preserving each panel's authored position.
    /// Владеет переиспользуемыми переходами прозрачности и сдвига в независимом времени, сохраняя исходную позицию панели.
    /// </summary>
    public sealed class PanelTransition : MonoBehaviour
    {
        private const float Duration = 0.2f;
        private static readonly Vector2 HiddenOffset = new(0f, -20f);

        [SerializeField] private GameObject visibilityRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform animatedRect;
        [SerializeField] private Vector2 restingPosition;

        private Coroutine routine;

        public void Configure(GameObject root, CanvasGroup group, RectTransform panelRect)
        {
            visibilityRoot = root;
            canvasGroup = group;
            animatedRect = panelRect;
            restingPosition = panelRect != null ? panelRect.anchoredPosition : Vector2.zero;
        }

        public void SetVisible(bool visible)
        {
            if (visibilityRoot == null || canvasGroup == null)
            {
                if (visibilityRoot != null)
                {
                    visibilityRoot.SetActive(visible);
                }

                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            if (!visible && !visibilityRoot.activeSelf)
            {
                ApplyState(0f, HiddenOffset);
                SetInteraction(false);
                return;
            }

            if (visible)
            {
                visibilityRoot.SetActive(true);
                SetInteraction(true);
            }
            else
            {
                SetInteraction(false);
            }

            routine = StartCoroutine(Animate(visible));
        }

        private IEnumerator Animate(bool visible)
        {
            var startAlpha = canvasGroup.alpha;
            var startOffset = animatedRect != null
                ? animatedRect.anchoredPosition - restingPosition
                : Vector2.zero;
            var targetAlpha = visible ? 1f : 0f;
            var targetOffset = visible ? Vector2.zero : HiddenOffset;
            var elapsed = 0f;

            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / Duration);
                var eased = 1f - Mathf.Pow(1f - progress, 3f);
                ApplyState(
                    Mathf.Lerp(startAlpha, targetAlpha, eased),
                    Vector2.Lerp(startOffset, targetOffset, eased));
                yield return null;
            }

            ApplyState(targetAlpha, targetOffset);
            if (!visible)
            {
                visibilityRoot.SetActive(false);
            }

            routine = null;
        }

        private void ApplyState(float alpha, Vector2 offset)
        {
            canvasGroup.alpha = alpha;
            if (animatedRect != null)
            {
                animatedRect.anchoredPosition = restingPosition + offset;
            }
        }

        private void SetInteraction(bool enabled)
        {
            canvasGroup.blocksRaycasts = enabled;
            canvasGroup.interactable = enabled;
        }
    }
}
