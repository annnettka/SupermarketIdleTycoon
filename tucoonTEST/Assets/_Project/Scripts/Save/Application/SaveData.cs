using System;
using System.Collections.Generic;

namespace SupermarketTycoon.Save
{
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

    [Serializable]
    public sealed class SettingsData
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.75f;
        public float SfxVolume = 1f;
        public bool Fullscreen = true;
    }
}
