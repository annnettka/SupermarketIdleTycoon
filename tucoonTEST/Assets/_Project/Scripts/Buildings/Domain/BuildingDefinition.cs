using System;
using UnityEngine;

namespace SupermarketTycoon.Buildings
{
    public enum BuildingType
    {
        Shelf,
        Checkout,
        FutureExpansion
    }

    [Serializable]
    public struct BuildingLevelDefinition
    {
        [SerializeField, Min(1)] private int level;
        [SerializeField, Min(0)] private int upgradeCost;
        [SerializeField, Min(1)] private int requiredPlayerLevel;
        [SerializeField, Min(1)] private int capacity;
        [SerializeField, Min(0.01f)] private float incomeMultiplier;
        [SerializeField, Min(0.1f)] private float interactionDuration;
        [SerializeField, Min(0.1f)] private float visualScale;

        public int Level => Mathf.Max(1, level);
        public int UpgradeCost => Mathf.Max(0, upgradeCost);
        public int RequiredPlayerLevel => Mathf.Max(1, requiredPlayerLevel);
        public int Capacity => Mathf.Max(1, capacity);
        public float IncomeMultiplier => Mathf.Max(0.01f, incomeMultiplier);
        public float InteractionDuration => Mathf.Max(0.1f, interactionDuration);
        public float VisualScale => Mathf.Max(0.1f, visualScale);

        public BuildingLevelDefinition(
            int buildingLevel,
            int cost,
            int playerLevel,
            int stationCapacity,
            float paymentMultiplier,
            float duration,
            float scale)
        {
            level = buildingLevel;
            upgradeCost = cost;
            requiredPlayerLevel = playerLevel;
            capacity = stationCapacity;
            incomeMultiplier = paymentMultiplier;
            interactionDuration = duration;
            visualScale = scale;
        }
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
        [SerializeField] private BuildingLevelDefinition[] upgradeLevels;

        public string Id => id;
        public string DisplayName => displayName;
        public BuildingType Type => type;
        public GameObject Prefab => prefab;
        public int Cost => cost;
        public int RequiredLevel => requiredLevel;
        public int Capacity => capacity;
        public float IncomeMultiplier => incomeMultiplier;
        public int MaxLevel => upgradeLevels != null && upgradeLevels.Length > 0
            ? upgradeLevels.Length
            : 1;

        public BuildingLevelDefinition GetLevel(int buildingLevel)
        {
            if (upgradeLevels != null && upgradeLevels.Length > 0)
            {
                return upgradeLevels[Mathf.Clamp(buildingLevel - 1, 0, upgradeLevels.Length - 1)];
            }

            var duration = type == BuildingType.Checkout ? 2.5f : 2.25f;
            return new BuildingLevelDefinition(
                1,
                0,
                requiredLevel,
                capacity,
                incomeMultiplier,
                duration,
                1f);
        }

        public bool TryGetNextLevel(int currentLevel, out BuildingLevelDefinition nextLevel)
        {
            if (currentLevel >= MaxLevel)
            {
                nextLevel = default;
                return false;
            }

            nextLevel = GetLevel(currentLevel + 1);
            return true;
        }
    }
}
