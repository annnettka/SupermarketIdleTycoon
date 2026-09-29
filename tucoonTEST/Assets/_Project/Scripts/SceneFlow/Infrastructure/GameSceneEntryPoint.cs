using SupermarketTycoon.Buildings;
using SupermarketTycoon.Core;
using SupermarketTycoon.Customers;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Progression;
using SupermarketTycoon.Save;
using SupermarketTycoon.UI;
using UnityEngine;

namespace SupermarketTycoon.SceneFlow
{
    public sealed class GameSceneEntryPoint : SceneEntryPoint
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private CustomerConfig customerConfig;
        [SerializeField] private ProgressionConfig progressionConfig;

        [Header("Scene")]
        [SerializeField] private BuildSpot[] buildSpots;
        [SerializeField] private CustomerSpawner customerSpawner;
        [SerializeField] private GameHudView hudView;
        [SerializeField] private PauseMenuView pauseView;
        [SerializeField] private SettingsView settingsView;

        private ApplicationContext applicationContext;
        private PauseService pause;
        private BuildingService buildings;
        private CustomerSpawner customers;
        private GameSaveCoordinator saveCoordinator;
        private SettingsController settingsController;
        private PauseController pauseController;
        private HudController hudController;
        private bool initialized;
        private bool suppressFinalSave;

        public void Configure(
            GameConfig game,
            CustomerConfig customer,
            ProgressionConfig progression,
            BuildSpot[] spots,
            CustomerSpawner spawner,
            GameHudView hud,
            PauseMenuView pauseMenu,
            SettingsView settings)
        {
            gameConfig = game;
            customerConfig = customer;
            progressionConfig = progression;
            buildSpots = spots;
            customerSpawner = spawner;
            hudView = hud;
            pauseView = pauseMenu;
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
            var saveData = context.SaveRepository.LoadOrCreate(gameConfig);
            var wallet = new Wallet(saveData.Money);
            var progression = new ProgressionService(
                progressionConfig,
                saveData.CurrentLevel,
                saveData.CurrentXp);
            var economy = new EconomyService(wallet, gameConfig.CustomerPayment);
            var stations = new StationRegistry();
            pause = new PauseService();
            buildings = new BuildingService(wallet, progression, stations, context.Audio);

            for (var i = 0; i < buildSpots.Length; i++)
            {
                buildings.Register(buildSpots[i]);
            }

            buildings.Restore(saveData.BuiltBuildings);

            customers = customerSpawner;
            customers.Initialize(
                stations,
                economy,
                progression,
                pause,
                customerConfig,
                gameConfig.CustomerXpReward,
                gameConfig.SpawnInterval,
                gameConfig.MaximumActiveCustomers);

            saveCoordinator = new GameSaveCoordinator(
                context.SaveRepository,
                wallet,
                progression,
                buildings);
            settingsController = new SettingsController(settingsView, context, ResetProgress);
            pauseController = new PauseController(
                pauseView,
                context,
                pause,
                saveCoordinator,
                settingsController);
            hudController = new HudController(
                hudView,
                wallet,
                progression,
                customers,
                buildings,
                stations,
                context.Audio,
                pauseController.Open);

            initialized = true;
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
            hudController?.Dispose();
            pauseController?.Dispose();
            settingsController?.Dispose();
            saveCoordinator?.Dispose();
            customers?.Shutdown();
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
