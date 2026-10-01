using System;
using UnityEngine;

namespace SupermarketTycoon.Employees
{
    /// <summary>
    /// Defines one employee tier with its unlock, purchase cost, and automation bonus.
    /// Определяет один уровень сотрудника с разблокировкой, стоимостью и бонусом автоматизации.
    /// </summary>
    [Serializable]
    public struct EmployeeLevelDefinition
    {
        [SerializeField, Min(1)] private int level;
        [SerializeField, Min(0)] private int cost;
        [SerializeField, Min(1)] private int requiredPlayerLevel;
        [SerializeField, Range(0f, 2f)] private float checkoutSpeedBonus;

        public int Level => Mathf.Max(1, level);
        public int Cost => Mathf.Max(0, cost);
        public int RequiredPlayerLevel => Mathf.Max(1, requiredPlayerLevel);
        public float CheckoutSpeedBonus => Mathf.Max(0f, checkoutSpeedBonus);

        public EmployeeLevelDefinition(int employeeLevel, int price, int playerLevel, float speedBonus)
        {
            level = employeeLevel;
            cost = price;
            requiredPlayerLevel = playerLevel;
            checkoutSpeedBonus = speedBonus;
        }
    }

    /// <summary>
    /// Stores stable employee identity and ordered automation levels as configuration-only data.
    /// Хранит стабильную идентичность сотрудника и упорядоченные уровни автоматизации только как конфигурацию.
    /// </summary>
    [CreateAssetMenu(menuName = "Supermarket Tycoon/Employee Definition", fileName = "CashierEmployee")]
    public sealed class EmployeeDefinition : ScriptableObject
    {
        [SerializeField] private string id = "employee.cashier";
        [SerializeField] private string displayName = "Cashier";
        [SerializeField] private EmployeeLevelDefinition[] levels;

        public string Id => id;
        public string DisplayName => displayName;
        public int MaxLevel => levels == null ? 0 : levels.Length;

        public EmployeeLevelDefinition GetLevel(int level)
        {
            if (levels == null || levels.Length == 0)
            {
                return new EmployeeLevelDefinition(1, 300, 2, 0.2f);
            }

            return levels[Mathf.Clamp(level - 1, 0, levels.Length - 1)];
        }
    }
}
