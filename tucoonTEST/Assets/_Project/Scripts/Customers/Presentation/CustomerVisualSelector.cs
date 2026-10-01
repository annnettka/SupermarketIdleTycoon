using System;
using UnityEngine;

namespace SupermarketTycoon.Customers
{
    /// <summary>
    /// Associates a stable visual ID with one level-gated character root.
    /// Связывает стабильный ID внешности с одним корневым объектом персонажа, ограниченным уровнем.
    /// </summary>
    [Serializable]
    public sealed class CustomerVisualVariant
    {
        [SerializeField] private string id;
        [SerializeField] private GameObject root;
        [SerializeField, Min(1)] private int requiredLevel = 1;

        public CustomerVisualVariant(string variantId, GameObject visualRoot, int playerLevel = 1)
        {
            id = variantId;
            root = visualRoot;
            requiredLevel = Mathf.Max(1, playerLevel);
        }

        public string Id => id;
        public GameObject Root => root;
        public int RequiredLevel => Mathf.Max(1, requiredLevel);
    }

    /// <summary>
    /// Selects an eligible character appearance and applies profile tint through MaterialPropertyBlock.
    /// Выбирает доступную внешность персонажа и применяет оттенок профиля через MaterialPropertyBlock.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CustomerVisualSelector : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private CustomerVisualVariant[] variants;

        private MaterialPropertyBlock propertyBlock;
        private int activeIndex = -1;

        public int VariantCount => variants != null ? variants.Length : 0;
        public string ActiveVariantId => activeIndex >= 0 && activeIndex < VariantCount
            ? variants[activeIndex].Id
            : string.Empty;

        public void Configure(CustomerVisualVariant[] visualVariants)
        {
            variants = visualVariants;
            SetActiveVariant(-1);
        }

        public void Select(int playerLevel, Color profileTint)
        {
            var availableCount = CountAvailable(playerLevel);
            if (availableCount == 0)
            {
                SetActiveVariant(-1);
                return;
            }

            var selection = UnityEngine.Random.Range(0, availableCount);
            var selectedIndex = FindAvailableIndex(playerLevel, selection);
            if (availableCount > 1 && selectedIndex == activeIndex)
            {
                selectedIndex = FindAvailableIndex(playerLevel, (selection + 1) % availableCount);
            }

            SetActiveVariant(selectedIndex);
            ApplyTint(profileTint);
        }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        private int CountAvailable(int playerLevel)
        {
            var count = 0;
            if (variants == null)
            {
                return count;
            }

            for (var i = 0; i < variants.Length; i++)
            {
                var variant = variants[i];
                if (variant != null && variant.Root != null && playerLevel >= variant.RequiredLevel)
                {
                    count++;
                }
            }

            return count;
        }

        private int FindAvailableIndex(int playerLevel, int availableIndex)
        {
            for (var i = 0; i < variants.Length; i++)
            {
                var variant = variants[i];
                if (variant == null || variant.Root == null || playerLevel < variant.RequiredLevel)
                {
                    continue;
                }

                if (availableIndex == 0)
                {
                    return i;
                }

                availableIndex--;
            }

            return -1;
        }

        private void SetActiveVariant(int selectedIndex)
        {
            activeIndex = selectedIndex;
            if (variants == null)
            {
                return;
            }

            for (var i = 0; i < variants.Length; i++)
            {
                var root = variants[i]?.Root;
                if (root != null)
                {
                    root.SetActive(i == selectedIndex);
                }
            }
        }

        private void ApplyTint(Color color)
        {
            if (activeIndex < 0 || activeIndex >= VariantCount || variants[activeIndex]?.Root == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();

            var renderers = variants[activeIndex].Root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
