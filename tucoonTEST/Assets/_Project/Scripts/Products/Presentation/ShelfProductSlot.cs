using System.Collections;
using UnityEngine;

namespace SupermarketTycoon.Products
{
    [DisallowMultipleComponent]
    public sealed class ShelfProductSlot : MonoBehaviour
    {
        [SerializeField] private ProductDefinition definition;
        [SerializeField] private GameObject visualRoot;

        private Coroutine animationRoutine;
        private Vector3 visualScale = Vector3.one;
        private bool unlocked = true;
        private bool available = true;
        private bool reserved;

        public ProductDefinition Definition => definition;
        public bool IsAvailable => unlocked && available && !reserved && definition != null && visualRoot != null;

        public void Configure(ProductDefinition product, GameObject visual)
        {
            definition = product;
            visualRoot = visual;
            visualScale = visualRoot != null ? visualRoot.transform.localScale : Vector3.one;
            available = true;
            reserved = false;
            RefreshVisual();
        }

        public void SetUnlocked(bool value)
        {
            unlocked = value;
            if (!unlocked)
            {
                reserved = false;
            }

            RefreshVisual();
        }

        public bool TryReserve()
        {
            if (!IsAvailable)
            {
                return false;
            }

            reserved = true;
            return true;
        }

        public void CancelReservation()
        {
            reserved = false;
        }

        public ProductDefinition Consume()
        {
            if (!unlocked || !available || !reserved)
            {
                return null;
            }

            reserved = false;
            available = false;
            PlayScaleAnimation(false);
            return definition;
        }

        public void Restock()
        {
            available = true;
            reserved = false;
            PlayScaleAnimation(true);
        }

        public void ResetState()
        {
            available = true;
            reserved = false;
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            RefreshVisual();
        }

        private void Awake()
        {
            if (visualRoot != null)
            {
                visualScale = visualRoot.transform.localScale;
            }

            RefreshVisual();
        }

        private void PlayScaleAnimation(bool appearing)
        {
            if (visualRoot == null)
            {
                return;
            }

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            animationRoutine = StartCoroutine(AnimateVisual(appearing));
        }

        private IEnumerator AnimateVisual(bool appearing)
        {
            if (appearing)
            {
                visualRoot.SetActive(unlocked);
            }

            var from = appearing ? Vector3.zero : visualScale;
            var to = appearing ? visualScale : Vector3.zero;
            var elapsed = 0f;
            const float duration = 0.16f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                visualRoot.transform.localScale = Vector3.LerpUnclamped(from, to, progress);
                yield return null;
            }

            visualRoot.transform.localScale = visualScale;
            visualRoot.SetActive(unlocked && available);
            animationRoutine = null;
        }

        private void RefreshVisual()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.transform.localScale = visualScale;
            visualRoot.SetActive(unlocked && available);
        }
    }
}
