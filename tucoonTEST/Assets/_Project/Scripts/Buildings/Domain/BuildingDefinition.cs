using UnityEngine;

namespace SupermarketTycoon.Buildings
{
    public enum BuildingType
    {
        Shelf,
        Checkout,
        FutureExpansion
    }

    [CreateAssetMenu(menuName = "Supermarket Tycoon/Building Definition", fileName = "BuildingDefinition")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private BuildingType type;
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0)] private int cost;
        [SerializeField, Min(1)] private int requiredLevel = 1;
        [SerializeField, Min(1)] private int capacity = 1;
        [SerializeField, Min(0.01f)] private float incomeMultiplier = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public BuildingType Type => type;
        public GameObject Prefab => prefab;
        public int Cost => cost;
        public int RequiredLevel => requiredLevel;
        public int Capacity => capacity;
        public float IncomeMultiplier => incomeMultiplier;
    }
}
