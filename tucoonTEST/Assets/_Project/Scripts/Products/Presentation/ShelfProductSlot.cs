using System.Collections;
using UnityEngine;

namespace SupermarketTycoon.Products
{
    /// <summary>
    /// Maps logical slot availability to four coarse visual fill states; it is not authoritative gameplay stock.
    /// Отображает логическую доступность слота четырьмя визуальными уровнями заполнения; это не авторитетный игровой запас.
    /// </summary>
    public enum ProductStockFill
    {
        Empty,
        Low,
        Medium,
        Full
    }

    /// <summary>
    /// Owns one logical product slot, its reservation, and the six-item display group used to visualize stock.
    /// Владеет одним логическим слотом товара, его резервированием и группой из шести предметов для отображения запаса.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShelfProductSlot : MonoBehaviour
    {
        [SerializeField] private ProductDefinition definition;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private GameObject[] visualItems;

        private Coroutine animationRoutine;
        private Vector3 visualScale = Vector3.one;
        private int visibleItemCount;
        private bool unlocked = true;
        private bool available = true;
        private bool reserved;

        public ProductDefinition Definition => definition;
        public bool IsAvailable => unlocked && available && !reserved && definition != null && visualRoot != null;
        public int VisibleItemCount => visibleItemCount;

        public void Configure(ProductDefinition product, GameObject visual, GameObject[] items = null)
        {
            definition = product;
            visualRoot = visual;
            visualItems = items != null && items.Length > 0
                ? items
                : visual != null ? new[] { visual } : new GameObject[0];
            visualScale = visualRoot != null ? visualRoot.transform.localScale : Vector3.one;
            visibleItemCount = visualItems.Length;
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
            visibleItemCount = 0;
            PlayScaleAnimation(false);
            return definition;
        }

        public void Restock()
        {
            var wasEmpty = visibleItemCount == 0;
            available = true;
            reserved = false;
            visibleItemCount = visualItems != null ? visualItems.Length : 0;
            if (wasEmpty)
            {
                PlayScaleAnimation(true);
            }
            else
            {
                RefreshVisual();
            }
        }

        public void SetFillState(ProductStockFill fill)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            var itemCount = visualItems != null ? visualItems.Length : 0;
            visibleItemCount = fill switch
            {
                ProductStockFill.Empty => 0,
                ProductStockFill.Low => Mathf.CeilToInt(itemCount / 3f),
                ProductStockFill.Medium => Mathf.CeilToInt(itemCount * 2f / 3f),
                _ => itemCount
            };
            RefreshVisual();
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

            visibleItemCount = visualItems != null ? visualItems.Length : 0;
            RefreshVisual();
        }

        private void Awake()
        {
            if (visualRoot != null)
            {
                visualScale = visualRoot.transform.localScale;
            }

            visibleItemCount = visualItems != null ? visualItems.Length : 0;
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
                ApplyItemVisibility();
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
            RefreshVisual();
            animationRoutine = null;
        }

        private void RefreshVisual()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.transform.localScale = visualScale;
            ApplyItemVisibility();
            visualRoot.SetActive(unlocked && visibleItemCount > 0);
        }

        private void ApplyItemVisibility()
        {
            if (visualItems == null)
            {
                return;
            }

            for (var i = 0; i < visualItems.Length; i++)
            {
                if (visualItems[i] != null && visualItems[i] != visualRoot)
                {
                    visualItems[i].SetActive(i < visibleItemCount);
                }
            }
        }
    }
}
