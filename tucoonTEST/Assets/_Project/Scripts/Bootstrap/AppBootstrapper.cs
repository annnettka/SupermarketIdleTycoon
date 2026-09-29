using SupermarketTycoon.Audio;
using SupermarketTycoon.Core;
using SupermarketTycoon.Save;
using SupermarketTycoon.SceneFlow;
using SupermarketTycoon.UI;
using UnityEngine;

namespace SupermarketTycoon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class AppBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private LoadingScreenView loadingScreen;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip buildClip;
        [SerializeField] private AudioClip incomeClip;
        [SerializeField] private AudioClip levelUpClip;

        private static bool instanceExists;
        private ApplicationContext context;

        private void Awake()
        {
            if (instanceExists)
            {
                Destroy(gameObject);
                return;
            }

            instanceExists = true;
            DontDestroyOnLoad(gameObject);
            ComposeApplication();
        }

        private void Start()
        {
            context.SceneFlow.LoadMainMenu();
        }

        private void OnDestroy()
        {
            if (context != null)
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
                musicSource,
                sfxSource,
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
