using System;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Progression;

namespace SupermarketTycoon.Save
{
    public sealed class GameSaveCoordinator : IDisposable
    {
        private readonly ISaveRepository repository;
        private readonly IWallet wallet;
        private readonly ProgressionService progression;
        private readonly BuildingService buildings;

        public GameSaveCoordinator(
            ISaveRepository repository,
            IWallet wallet,
            ProgressionService progression,
            BuildingService buildings)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));

            buildings.BuildingBuilt += OnBuildingBuilt;
            progression.XpChanged += OnXpChanged;
        }

        public void SaveNow()
        {
            repository.Save(new SaveData
            {
                SaveVersion = 1,
                Money = wallet.Balance,
                CurrentLevel = progression.CurrentLevel,
                CurrentXp = progression.CurrentXp,
                BuiltBuildings = buildings.CaptureBuiltBuildings()
            });
        }

        public void Dispose()
        {
            buildings.BuildingBuilt -= OnBuildingBuilt;
            progression.XpChanged -= OnXpChanged;
        }

        private void OnBuildingBuilt(BuildSpot _, BuildingDefinition __)
        {
            SaveNow();
        }

        private void OnXpChanged(int _, int __)
        {
            SaveNow();
        }
    }
}
