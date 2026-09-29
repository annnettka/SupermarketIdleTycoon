using UnityEngine;

namespace SupermarketTycoon.Progression
{
    [CreateAssetMenu(menuName = "Supermarket Tycoon/Progression Config", fileName = "ProgressionConfig")]
    public sealed class ProgressionConfig : ScriptableObject
    {
        [SerializeField] private int[] xpRequiredPerLevel = { 100, 250, 500 };

        public int GetRequiredXp(int currentLevel)
        {
            var safeLevel = Mathf.Max(1, currentLevel);
            if (xpRequiredPerLevel == null || xpRequiredPerLevel.Length == 0)
            {
                return safeLevel * 100;
            }

            var index = Mathf.Min(safeLevel - 1, xpRequiredPerLevel.Length - 1);
            return Mathf.Max(1, xpRequiredPerLevel[index]);
        }
    }
}
