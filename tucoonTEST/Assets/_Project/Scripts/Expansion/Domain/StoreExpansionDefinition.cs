using UnityEngine;

namespace SupermarketTycoon.Expansion
{
    /// <summary>
    /// Defines a purchasable business area by stable ID, price, and progression requirement.
    /// Определяет покупаемую деловую зону стабильным ID, ценой и требованием прогрессии.
    /// </summary>
    [CreateAssetMenu(menuName = "Supermarket Tycoon/Store Expansion", fileName = "StoreExpansion")]
    public sealed class StoreExpansionDefinition : ScriptableObject
    {
        [SerializeField] private string id = "expansion.main";
        [SerializeField] private string displayName = "Store Expansion";
        [SerializeField, Min(0)] private int cost = 500;
        [SerializeField, Min(1)] private int requiredLevel = 3;

        public string Id => id;
        public string DisplayName => displayName;
        public int Cost => Mathf.Max(0, cost);
        public int RequiredLevel => Mathf.Max(1, requiredLevel);
    }
}
