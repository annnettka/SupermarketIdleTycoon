using SupermarketTycoon.Buildings;
using SupermarketTycoon.Core;
using SupermarketTycoon.Customers;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Employees;
using SupermarketTycoon.Expansion;
using SupermarketTycoon.Objectives;
using SupermarketTycoon.Offline;
using SupermarketTycoon.Progression;
using SupermarketTycoon.Save;
using SupermarketTycoon.Stats;
using SupermarketTycoon.UI;
using UnityEngine;

namespace SupermarketTycoon.SceneFlow
{
    /// <summary>
    /// Composes session-lifetime gameplay services and controllers, then disposes them in reverse ownership order.
    /// Компонует игровые сервисы и контроллеры уровня сессии, затем освобождает их в обратном порядке владения.
    /// </summary>
    public sealed class GameSceneEntryPoint : SceneEntryPoint
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private CustomerConfig customerConfig;
        [SerializeField] private ProgressionConfig progressionConfig;
        [SerializeField] private CustomerProfileDefinition[] customerProfiles;
        [SerializeField] private ObjectiveConfig objectiveConfig;
        [SerializeField] private EmployeeDefinition employeeDefinition;

        [Header("Scene")]
        [SerializeField] private BuildSpot[] buildSpots;
        [SerializeField] private StoreExpansionSpot[] expansionSpots;
        [SerializeField] private GameObject employeeVisual;
        [SerializeField] private CustomerSpawner customerSpawner;
        [SerializeField] private GameHudView hudView;
        [SerializeField] private BuildingPanelView buildingPanelView;
        [SerializeField] private EmployeeView employeeView;
        [SerializeField] private OfflineIncomeView offlineIncomeView;
        [SerializeField] private PauseMenuView pauseView;
        [SerializeField] private StatsView statsView;
        [SerializeField] private SettingsView settingsView;

        private ApplicationContext applicationContext;
        private PauseService pause;
        private BuildingService buildings;
        private BuildingUpgradeService upgrades;
        private StoreExpansionService expansions;
        private EmployeeService employees;
        private StoreRatingService rating;
        private ObjectiveService objectives;
        private StatisticsService stats;
        private OfflineIncomeService offline;
        private EconomyService economy;
        private ProgressionService progression;
        private CustomerSpawner customers;
        private GameSessionEventCoordinator eventCoordinator;
        private GameSaveCoordinator saveCoordinator;
        private SettingsController settingsController;
        private PauseController pauseController;
        private HudController hudController;
        private BuildingPanelController buildingPanelController;
        private EmployeeController employeeController;
        private OfflineIncomeController offlineController;
        private bool initialized;
        private bool suppressFinalSave;

        public void Configure(
            GameConfig game,
            CustomerConfig customer,
            ProgressionConfig progressionConfigAsset,
            CustomerProfileDefinition[] profiles,
            ObjectiveConfig objectivesConfig,
            EmployeeDefinition cashierDefinition,
            BuildSpot[] spots,
            StoreExpansionSpot[] storeExpansionSpots,
            GameObject cashierVisual,
            CustomerSpawner spawner,
            GameHudView hud,
            BuildingPanelView buildingPanel,
            EmployeeView employeePanel,
            OfflineIncomeView offlinePanel,
            PauseMenuView pauseMenu,
            StatsView statisticsView,
            SettingsView settings)
        {
            gameConfig = game;
            customerConfig = customer;
            progressionConfig = progressionConfigAsset;
            customerProfiles = profiles;
            objectiveConfig = objectivesConfig;
            employeeDefinition = cashierDefinition;
            buildSpots = spots;
            expansionSpots = storeExpansionSpots;
            employeeVisual = cashierVisual;
            customerSpawner = spawner;
            hudView = hud;
            buildingPanelView = buildingPanel;
            employeeView = employeePanel;
            offlineIncomeView = offlinePanel;
            pauseView = pauseMenu;
            statsView = statisticsView;
            settingsView = settings;
        }

        public override void Initialize(ApplicationContext context)
        {
            if (initialized)
            {
                Debug.LogWarning("GameSceneEntryPoint was initialized more than once.", this);
                return;
            }

            applicationContext = context;
            context.Audio.PlayGameplayMusic();
            var saveData = context.SaveRepository.LoadOrCreate(gameConfig);
            var wallet = new Wallet(saveData.Money);
            progression = new ProgressionService(progressionConfig, saveData.CurrentLevel, saveData.CurrentXp);
            rating = new StoreRatingService(saveData.StoreRating);
            economy = new EconomyService(wallet, gameConfig.CustomerPayment);
            var stations = new StationRegistry();
            pause = new PauseService();
            buildings = new BuildingService(wallet, progression, stations, context.Audio);
            upgrades = new BuildingUpgradeService(wallet, progression, buildings, context.Audio);

            for (var i = 0; i < buildSpots.Length; i++)
            {
                buildings.Register(buildSpots[i]);
            }

            expansions = new StoreExpansionService(wallet, progression, buildings, context.Audio);
            for (var i = 0; i < expansionSpots.Length; i++)
            {
                expansions.Register(expansionSpots[i]);
            }

            expansions.Restore(saveData.PurchasedExpansionIds);
            employees = new EmployeeService(employeeDefinition, wallet, progression, stations, context.Audio, employeeVisual);
            employees.Restore(saveData.CashierLevel);
            buildings.Restore(saveData.BuiltBuildings);

            stats = new StatisticsService(saveData.LifetimeStats);
            objectives = new ObjectiveService(
                objectiveConfig,
                economy,
                progression,
                saveData.CurrentObjectiveIndex,
                saveData.CurrentObjectiveProgress);
            offline = new OfflineIncomeService(
                buildings,
                employees,
                economy,
                saveData.LastSaveUtcTicks,
                saveData.PendingOfflineIncome);

            customers = customerSpawner;
            customers.Initialize(
                stations,
                economy,
                progression,
                rating,
                pause,
                customerConfig,
                customerProfiles,
                gameConfig.CustomerXpReward,
                gameConfig.SpawnInterval,
                gameConfig.MaximumActiveCustomers);

            eventCoordinator = new GameSessionEventCoordinator(
                buildings,
                upgrades,
                expansions,
                employees,
                economy,
                customers,
                progression,
                rating,
                objectives,
                stats);
            objectives.SynchronizeExistingState(buildings, employees, expansions, rating);

            saveCoordinator = new GameSaveCoordinator(
                context.SaveRepository,
                wallet,
                progression,
                buildings,
                expansions,
                employees,
                rating,
                objectives,
                stats,
                offline);
            settingsController = new SettingsController(settingsView, context, ResetProgress);
            pauseController = new PauseController(
                pauseView,
                statsView,
                stats,
                context,
                pause,
                saveCoordinator,
                settingsController);
            hudController = new HudController(
                hudView,
                wallet,
                progression,
                rating,
                objectives,
                customers,
                context.Audio,
                pauseController.Open);
            buildingPanelController = new BuildingPanelController(
                buildingPanelView,
                buildings,
                upgrades,
                wallet,
                progression,
                gameConfig.CustomerPayment,
                pause,
                context.Audio);
            employeeController = new EmployeeController(employeeView, employees, wallet, progression);
            offlineController = new OfflineIncomeController(offlineIncomeView, offline);

            initialized = true;
            saveCoordinator.SaveNow();
        }

        private void Update()
        {
            if (initialized)
            {
                saveCoordinator.Tick(Time.unscaledDeltaTime);
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (initialized && isPaused)
            {
                saveCoordinator.SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            if (initialized && !suppressFinalSave)
            {
                saveCoordinator.SaveNow();
            }
        }

        private void OnDestroy()
        {
            if (initialized && !suppressFinalSave)
            {
                saveCoordinator.SaveNow();
            }

            pause?.SetPaused(false);
            offlineController?.Dispose();
            employeeController?.Dispose();
            buildingPanelController?.Dispose();
            hudController?.Dispose();
            pauseController?.Dispose();
            settingsController?.Dispose();
            saveCoordinator?.Dispose();
            eventCoordinator?.Dispose();
            customers?.Shutdown();
            expansions?.Dispose();
            buildings?.Dispose();
        }

        private void ResetProgress()
        {
            suppressFinalSave = true;
            applicationContext.SaveRepository.Delete();
            pause.SetPaused(false);
            applicationContext.SceneFlow.LoadMainMenu();
        }
    }
}
