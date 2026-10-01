using SupermarketTycoon.Audio;
using SupermarketTycoon.Core;
using SupermarketTycoon.Save;
using SupermarketTycoon.SceneFlow;
using SupermarketTycoon.UI;
using UnityEngine;
using UnityEngine.Audio;

namespace SupermarketTycoon.Bootstrap
{
    /// <summary>
    /// Owns the application composition root and constructs long-lived services with explicit dependencies.
    /// Владеет корнем композиции приложения и создает долгоживущие сервисы с явными зависимостями.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private LoadingScreenView loadingScreen;
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource secondaryMusicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip menuMusicClip;
        [SerializeField] private AudioClip gameplayMusicClip;
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip buildClip;
        [SerializeField] private AudioClip incomeClip;
        [SerializeField] private AudioClip levelUpClip;

        private static bool instanceExists;
        private ApplicationContext context;
        private bool ownsInstance;

        private void Awake()
        {
            if (instanceExists)
            {
                Destroy(gameObject);
                return;
            }

            instanceExists = true;
            ownsInstance = true;
            DontDestroyOnLoad(gameObject);
            ComposeApplication();
        }

        private void Start()
        {
            context?.SceneFlow.LoadMainMenu();
        }

        private void OnDestroy()
        {
            if (ownsInstance)
            {
                instanceExists = false;
            }
        }

        private void ComposeApplication()
        {
            if (gameConfig == null)
            {
                Debug.LogError("AppBootstrapper requires a GameConfig.", this);
                enabled = false;
                return;
            }

            var audio = new AudioService(
                this,
                audioMixer,
                musicSource,
                secondaryMusicSource,
                sfxSource,
                menuMusicClip,
                gameplayMusicClip,
                uiClickClip,
                buildClip,
                incomeClip,
                levelUpClip);
            var settingsRepository = new SettingsRepository(Application.persistentDataPath);
            var settings = new SettingsService(settingsRepository, audio);
            var saveRepository = new JsonSaveRepository(Application.persistentDataPath);
            var sceneFlow = new SceneFlowService(this, loadingScreen, gameConfig.MinimumLoadingDuration);

            context = new ApplicationContext(saveRepository, settings, audio, sceneFlow);
            sceneFlow.AttachContext(context);
        }
    }
}
