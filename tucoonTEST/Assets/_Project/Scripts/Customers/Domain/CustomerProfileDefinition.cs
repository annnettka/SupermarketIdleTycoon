using UnityEngine;

namespace SupermarketTycoon.Customers
{
    [CreateAssetMenu(menuName = "Supermarket Tycoon/Customer Profile", fileName = "CustomerProfile")]
    public sealed class CustomerProfileDefinition : ScriptableObject
    {
        [SerializeField] private string id = "customer.normal";
        [SerializeField] private string displayName = "Normal";
        [SerializeField, Min(0.1f)] private float movementSpeedMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float shoppingTimeMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float paymentMultiplier = 1f;
        [SerializeField, Min(1f)] private float queuePatience = 15f;
        [SerializeField, Min(0f)] private float spawnWeight = 1f;
        [SerializeField, Min(1)] private int requiredLevel = 1;
        [SerializeField, Range(0f, 1f)] private float premiumShelfPreference = 0.1f;
        [SerializeField] private Color presentationColor = Color.white;

        public string Id => id;
        public string DisplayName => displayName;
        public float MovementSpeedMultiplier => Mathf.Max(0.1f, movementSpeedMultiplier);
        public float ShoppingTimeMultiplier => Mathf.Max(0.1f, shoppingTimeMultiplier);
        public float PaymentMultiplier => Mathf.Max(0.1f, paymentMultiplier);
        public float QueuePatience => Mathf.Max(1f, queuePatience);
        public float SpawnWeight => Mathf.Max(0f, spawnWeight);
        public int RequiredLevel => Mathf.Max(1, requiredLevel);
        public float PremiumShelfPreference => Mathf.Clamp01(premiumShelfPreference);
        public Color PresentationColor => presentationColor;
    }
}
