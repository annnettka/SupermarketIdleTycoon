using UnityEngine;

namespace SupermarketTycoon.Products
{
    public enum ProductCategory
    {
        Fruit,
        Vegetables,
        Bread,
        Snacks,
        Drinks,
        PackagedFood,
        Premium
    }

    [CreateAssetMenu(menuName = "Supermarket Tycoon/Product Definition", fileName = "ProductDefinition")]
    public sealed class ProductDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private ProductCategory category;
        [SerializeField, Min(1)] private int baseValue = 10;
        [SerializeField, Min(0.1f)] private float valueMultiplier = 1f;
        [SerializeField, Min(1)] private int requiredStoreLevel = 1;
        [SerializeField] private bool premium;

        public string Id => id;
        public string DisplayName => displayName;
        public GameObject VisualPrefab => visualPrefab;
        public ProductCategory Category => category;
        public int BaseValue => Mathf.Max(1, baseValue);
        public float ValueMultiplier => Mathf.Max(0.1f, valueMultiplier);
        public int RequiredStoreLevel => Mathf.Max(1, requiredStoreLevel);
        public bool IsPremium => premium;
    }
}
