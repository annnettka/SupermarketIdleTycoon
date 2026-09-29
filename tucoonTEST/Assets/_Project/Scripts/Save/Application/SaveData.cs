using System;
using System.Collections.Generic;

namespace SupermarketTycoon.Save
{
    [Serializable]
    public sealed class SaveData
    {
        public int SaveVersion = 1;
        public int Money;
        public int CurrentLevel = 1;
        public int CurrentXp;
        public List<BuiltBuildingData> BuiltBuildings = new List<BuiltBuildingData>();
    }

    [Serializable]
    public sealed class BuiltBuildingData
    {
        public BuiltBuildingData()
        {
        }

        public BuiltBuildingData(string buildSpotId, string buildingDefinitionId)
        {
            BuildSpotId = buildSpotId;
            BuildingDefinitionId = buildingDefinitionId;
        }

        public string BuildSpotId;
        public string BuildingDefinitionId;
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
