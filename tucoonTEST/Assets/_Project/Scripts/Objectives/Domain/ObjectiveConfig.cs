using System;
using UnityEngine;

namespace SupermarketTycoon.Objectives
{
    public enum ObjectiveType
    {
        BuildBuilding,
        UpgradeBuilding,
        ServeCustomers,
        EarnMoney,
        ReachLevel,
        BuyEmployee,
        BuyExpansion,
        ReachRating
    }

    [Serializable]
    public sealed class ObjectiveDefinition
    {
        [SerializeField] private ObjectiveType type;
        [SerializeField] private string displayName;
        [SerializeField] private string filterId;
        [SerializeField, Min(1)] private int target = 1;
        [SerializeField, Min(0)] private int moneyReward;
        [SerializeField, Min(0)] private int xpReward;

        public ObjectiveType Type => type;
        public string DisplayName => displayName;
        public string FilterId => filterId;
        public int Target => Mathf.Max(1, target);
        public int MoneyReward => Mathf.Max(0, moneyReward);
        public int XpReward => Mathf.Max(0, xpReward);

        public ObjectiveDefinition(
            ObjectiveType objectiveType,
            string title,
            int goal,
            string requiredId = null,
            int rewardMoney = 0,
            int rewardXp = 0)
        {
            type = objectiveType;
            displayName = title;
            filterId = requiredId;
            target = goal;
            moneyReward = rewardMoney;
            xpReward = rewardXp;
        }
    }

    [CreateAssetMenu(menuName = "Supermarket Tycoon/Objective Config", fileName = "ObjectiveConfig")]
    public sealed class ObjectiveConfig : ScriptableObject
    {
        [SerializeField] private ObjectiveDefinition[] objectives;

        public int Count => objectives == null ? 0 : objectives.Length;

        public ObjectiveDefinition Get(int index)
        {
            return objectives != null && index >= 0 && index < objectives.Length
                ? objectives[index]
                : null;
        }

        public void Configure(ObjectiveDefinition[] definitions)
        {
            objectives = definitions;
        }
    }
}
