using System;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Customers;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Employees;
using SupermarketTycoon.Expansion;
using SupermarketTycoon.Objectives;
using SupermarketTycoon.Progression;
using SupermarketTycoon.Stats;
using UnityEngine;

namespace SupermarketTycoon.Core
{
    /// <summary>
    /// Routes domain events between independent session services and owns every cross-system subscription.
    /// Направляет доменные события между независимыми сервисами сессии и владеет всеми межсистемными подписками.
    /// </summary>
    public sealed class GameSessionEventCoordinator : IDisposable
    {
        private readonly BuildingService buildings;
        private readonly BuildingUpgradeService upgrades;
        private readonly StoreExpansionService expansions;
        private readonly EmployeeService employees;
        private readonly EconomyService economy;
        private readonly CustomerSpawner customers;
        private readonly ProgressionService progression;
        private readonly StoreRatingService rating;
        private readonly ObjectiveService objectives;
        private readonly StatisticsService statistics;

        public GameSessionEventCoordinator(
            BuildingService buildingService,
            BuildingUpgradeService upgradeService,
            StoreExpansionService expansionService,
            EmployeeService employeeService,
            EconomyService economyService,
            CustomerSpawner customerSpawner,
            ProgressionService progressionService,
            StoreRatingService ratingService,
            ObjectiveService objectiveService,
            StatisticsService statisticsService)
        {
            buildings = buildingService ?? throw new ArgumentNullException(nameof(buildingService));
            upgrades = upgradeService ?? throw new ArgumentNullException(nameof(upgradeService));
            expansions = expansionService ?? throw new ArgumentNullException(nameof(expansionService));
            employees = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            economy = economyService ?? throw new ArgumentNullException(nameof(economyService));
            customers = customerSpawner != null
                ? customerSpawner
                : throw new ArgumentNullException(nameof(customerSpawner));
            progression = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
            rating = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            objectives = objectiveService ?? throw new ArgumentNullException(nameof(objectiveService));
            statistics = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));

            buildings.BuildingBuilt += OnBuildingBuilt;
            upgrades.Upgraded += OnBuildingUpgraded;
            expansions.Purchased += OnExpansionPurchased;
            employees.Changed += OnEmployeeChanged;
            economy.IncomeAdded += OnIncomeAdded;
            customers.PaymentCompleted += OnCustomerServed;
            customers.CustomerLost += OnCustomerLost;
            progression.LevelChanged += OnLevelChanged;
            rating.Changed += OnRatingChanged;
        }

        public void Dispose()
        {
            buildings.BuildingBuilt -= OnBuildingBuilt;
            upgrades.Upgraded -= OnBuildingUpgraded;
            expansions.Purchased -= OnExpansionPurchased;
            employees.Changed -= OnEmployeeChanged;
            economy.IncomeAdded -= OnIncomeAdded;
            customers.PaymentCompleted -= OnCustomerServed;
            customers.CustomerLost -= OnCustomerLost;
            progression.LevelChanged -= OnLevelChanged;
            rating.Changed -= OnRatingChanged;
        }

        private void OnBuildingBuilt(BuildSpot _, BuildingDefinition definition)
        {
            statistics.RecordBuildingPurchased();
            objectives.RecordBuildingBuilt(definition);
        }

        private void OnBuildingUpgraded(BuildSpot spot, int _)
        {
            statistics.RecordUpgradePurchased();
            rating.RecordStoreUpgrade();
            objectives.RecordBuildingUpgraded(spot.Definition);
        }

        private void OnExpansionPurchased(StoreExpansionDefinition definition)
        {
            rating.RecordStoreUpgrade();
            objectives.RecordExpansionPurchased(definition.Id);
        }

        private void OnEmployeeChanged()
        {
            if (employees.CurrentLevel > 0)
            {
                objectives.RecordEmployeePurchased(employees.Definition.Id);
            }
        }

        private void OnIncomeAdded(int amount)
        {
            statistics.RecordIncome(amount);
            objectives.RecordMoneyEarned(amount);
        }

        private void OnCustomerServed(Vector3 _, int __)
        {
            statistics.RecordCustomerServed();
            rating.RecordSuccessfulTransaction();
            objectives.RecordCustomerServed();
        }

        private void OnCustomerLost(Vector3 _)
        {
            statistics.RecordCustomerLost();
            rating.RecordCustomerLost();
        }

        private void OnLevelChanged(int _)
        {
            objectives.RefreshLevel();
        }

        private void OnRatingChanged(float value)
        {
            objectives.RefreshRating(value);
        }
    }
}
