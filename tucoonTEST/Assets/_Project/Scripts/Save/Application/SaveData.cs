using System;
using System.Collections.Generic;

namespace SupermarketTycoon.Save
{
    /// <summary>
    /// Defines save schema version 2 for gameplay progress stored under Application.persistentDataPath.
    /// Определяет версию 2 схемы игрового прогресса, хранящегося в Application.persistentDataPath.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;

        public int SaveVersion = CurrentVersion;
        public int Money;
        public int CurrentLevel = 1;
        public int CurrentXp;
        public List<BuiltBuildingData> BuiltBuildings = new List<BuiltBuildingData>();
        public List<string> PurchasedExpansionIds = new List<string>();
        public int CashierLevel;
        public float StoreRating = 3f;
        public int CurrentObjectiveIndex;
        public int CurrentObjectiveProgress;
        public LifetimeStatsData LifetimeStats = new LifetimeStatsData();
        public long LastSaveUtcTicks;
        public int PendingOfflineIncome;
    }

    /// <summary>
    /// Persists one constructed building by stable scene and definition identifiers rather than object references.
    /// Сохраняет одно построенное здание по стабильным идентификаторам сцены и определения вместо ссылок на объекты.
    /// </summary>
    [Serializable]
    public sealed class BuiltBuildingData
    {
        public BuiltBuildingData()
        {
        }

        public BuiltBuildingData(string buildSpotId, string buildingDefinitionId, int buildingLevel = 1)
        {
            BuildSpotId = buildSpotId;
            BuildingDefinitionId = buildingDefinitionId;
            BuildingLevel = Math.Max(1, buildingLevel);
        }

        public string BuildSpotId;
        public string BuildingDefinitionId;
        public int BuildingLevel = 1;
    }

    /// <summary>
    /// Stores cumulative statistics independently from scene objects so they survive session replacement.
    /// Хранит накопительную статистику независимо от объектов сцены, чтобы она переживала замену сессии.
    /// </summary>
    [Serializable]
    public sealed class LifetimeStatsData
    {
        public int CustomersServed;
        public int CustomersLost;
        public long TotalMoneyEarned;
        public int TotalBuildingsPurchased;
        public int TotalUpgradesPurchased;

        public LifetimeStatsData Clone()
        {
            return new LifetimeStatsData
            {
                CustomersServed = CustomersServed,
                CustomersLost = CustomersLost,
                TotalMoneyEarned = TotalMoneyEarned,
                TotalBuildingsPurchased = TotalBuildingsPurchased,
                TotalUpgradesPurchased = TotalUpgradesPurchased
            };
        }
    }

    /// <summary>
    /// Stores application preferences in a schema independent from gameplay reset and migration.
    /// Хранит настройки приложения в схеме, независимой от сброса и миграции игрового прогресса.
    /// </summary>
    [Serializable]
    public sealed class SettingsData
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.75f;
        public float SfxVolume = 1f;
        public bool Fullscreen = true;
    }
}
