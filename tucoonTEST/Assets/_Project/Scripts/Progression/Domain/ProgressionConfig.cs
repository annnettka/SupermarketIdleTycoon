using UnityEngine;

namespace SupermarketTycoon.Progression
{
    [CreateAssetMenu(menuName = "Supermarket Tycoon/Progression Config", fileName = "ProgressionConfig")]
    public sealed class ProgressionConfig : ScriptableObject
    {
        [SerializeField] private int[] xpRequiredPerLevel = { 100, 200, 350, 450 };
        [SerializeField] private string[] unlockSummaries =
        {
            "Shelf and Checkout",
            "Second Shelf and Cashier",
            "Store Expansion and Second Checkout",
            "VIP Customers and Shelf Level 3",
            "Maximum Customer Flow"
        };

        public int MaxLevel => xpRequiredPerLevel == null || xpRequiredPerLevel.Length == 0
            ? 1
            : xpRequiredPerLevel.Length + 1;

        public int GetRequiredXp(int currentLevel)
        {
            if (currentLevel >= MaxLevel)
            {
                return 0;
            }

            var safeLevel = Mathf.Max(1, currentLevel);
            if (xpRequiredPerLevel == null || xpRequiredPerLevel.Length == 0)
            {
                return safeLevel * 100;
            }

            return Mathf.Max(1, xpRequiredPerLevel[safeLevel - 1]);
        }

        public string GetUnlockSummary(int level)
        {
            if (unlockSummaries == null || unlockSummaries.Length == 0)
            {
                return string.Empty;
            }

            var index = Mathf.Clamp(level - 1, 0, unlockSummaries.Length - 1);
            return unlockSummaries[index] ?? string.Empty;
        }
    }
}
