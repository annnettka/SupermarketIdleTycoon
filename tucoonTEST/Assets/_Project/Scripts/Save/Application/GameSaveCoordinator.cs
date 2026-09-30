using System;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Employees;
using SupermarketTycoon.Expansion;
using SupermarketTycoon.Objectives;
using SupermarketTycoon.Offline;
using SupermarketTycoon.Progression;
using SupermarketTycoon.Stats;

namespace SupermarketTycoon.Save
{
    public sealed class GameSaveCoordinator : IDisposable
    {
        private const float SaveDelaySeconds = 0.4f;

        private readonly ISaveRepository repository;
        private readonly IWallet wallet;
        private readonly ProgressionService progression;
        private readonly BuildingService buildings;
        private readonly StoreExpansionService expansions;
        private readonly EmployeeService employees;
        private readonly StoreRatingService rating;
        private readonly ObjectiveService objectives;
        private readonly StatisticsService stats;
        private readonly OfflineIncomeService offline;
        private bool dirty;
        private float saveDelay;

        public GameSaveCoordinator(
            ISaveRepository repository,
            IWallet wallet,
            ProgressionService progression,
            BuildingService buildings,
            StoreExpansionService expansions,
            EmployeeService employees,
            StoreRatingService rating,
            ObjectiveService objectives,
            StatisticsService stats,
            OfflineIncomeService offline)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            this.expansions = expansions ?? throw new ArgumentNullException(nameof(expansions));
            this.employees = employees ?? throw new ArgumentNullException(nameof(employees));
            this.rating = rating ?? throw new ArgumentNullException(nameof(rating));
            this.objectives = objectives ?? throw new ArgumentNullException(nameof(objectives));
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            this.offline = offline ?? throw new ArgumentNullException(nameof(offline));

            wallet.BalanceChanged += OnIntChanged;
            progression.XpChanged += OnXpChanged;
            buildings.BuildingBuilt += OnBuildingBuilt;
            buildings.BuildingLevelChanged += OnBuildingLevelChanged;
            expansions.StateChanged += MarkDirty;
            employees.Changed += MarkDirty;
            rating.Changed += OnFloatChanged;
            objectives.Changed += MarkDirty;
            stats.Changed += MarkDirty;
            offline.Changed += MarkDirty;
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!dirty)
            {
                return;
            }

            saveDelay -= unscaledDeltaTime;
            if (saveDelay <= 0f)
            {
                SaveNow();
            }
        }

        public void SaveNow()
        {
            repository.Save(new SaveData
            {
                SaveVersion = SaveData.CurrentVersion,
                Money = wallet.Balance,
                CurrentLevel = progression.CurrentLevel,
                CurrentXp = progression.CurrentXp,
                BuiltBuildings = buildings.CaptureBuiltBuildings(),
                PurchasedExpansionIds = expansions.CapturePurchasedIds(),
                CashierLevel = employees.CurrentLevel,
                StoreRating = rating.CurrentRating,
                CurrentObjectiveIndex = objectives.CurrentIndex,
                CurrentObjectiveProgress = objectives.CurrentProgress,
                LifetimeStats = stats.Capture(),
                LastSaveUtcTicks = offline.CaptureCurrentUtcTicks(),
                PendingOfflineIncome = offline.PendingIncome
            });
            dirty = false;
            saveDelay = 0f;
        }

        public void Dispose()
        {
            wallet.BalanceChanged -= OnIntChanged;
            progression.XpChanged -= OnXpChanged;
            buildings.BuildingBuilt -= OnBuildingBuilt;
            buildings.BuildingLevelChanged -= OnBuildingLevelChanged;
            expansions.StateChanged -= MarkDirty;
            employees.Changed -= MarkDirty;
            rating.Changed -= OnFloatChanged;
            objectives.Changed -= MarkDirty;
            stats.Changed -= MarkDirty;
            offline.Changed -= MarkDirty;
        }

        private void MarkDirty()
        {
            dirty = true;
            saveDelay = SaveDelaySeconds;
        }

        private void OnIntChanged(int _)
        {
            MarkDirty();
        }

        private void OnFloatChanged(float _)
        {
            MarkDirty();
        }

        private void OnXpChanged(int _, int __)
        {
            MarkDirty();
        }

        private void OnBuildingBuilt(BuildSpot _, BuildingDefinition __)
        {
            MarkDirty();
        }

        private void OnBuildingLevelChanged(BuildSpot _, int __)
        {
            MarkDirty();
        }
    }
}
