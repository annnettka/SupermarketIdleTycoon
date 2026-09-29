using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider progressBar;

        public void Configure(CanvasGroup group, Slider slider)
        {
            canvasGroup = group;
            progressBar = slider;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }

        public void SetProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.SetValueWithoutNotify(Mathf.Clamp01(progress));
            }
        }
    }
}
