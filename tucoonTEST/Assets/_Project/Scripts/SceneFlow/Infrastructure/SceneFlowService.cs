using System.Collections;
using SupermarketTycoon.Core;
using SupermarketTycoon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SupermarketTycoon.SceneFlow
{
    /// <summary>
    /// Serializes asynchronous scene transitions, drives loading feedback, and initializes one scene entry point.
    /// Последовательно выполняет асинхронные переходы сцен, обновляет экран загрузки и инициализирует одну точку входа сцены.
    /// </summary>
    public sealed class SceneFlowService
    {
        public const string MainMenuSceneName = "MainMenu";
        public const string GameSceneName = "Game";

        private readonly MonoBehaviour coroutineRunner;
        private readonly LoadingScreenView loadingScreen;
        private readonly float minimumVisibleDuration;
        private ApplicationContext context;
        private bool isLoading;

        public SceneFlowService(
            MonoBehaviour coroutineRunner,
            LoadingScreenView loadingScreen,
            float minimumVisibleDuration)
        {
            this.coroutineRunner = coroutineRunner;
            this.loadingScreen = loadingScreen;
            this.minimumVisibleDuration = Mathf.Max(0f, minimumVisibleDuration);
        }

        public bool IsLoading => isLoading;

        public void AttachContext(ApplicationContext applicationContext)
        {
            context = applicationContext;
        }

        public void LoadMainMenu()
        {
            Load(MainMenuSceneName);
        }

        public void LoadGame()
        {
            Load(GameSceneName);
        }

        private void Load(string sceneName)
        {
            if (isLoading || coroutineRunner == null)
            {
                return;
            }

            coroutineRunner.StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            isLoading = true;
            loadingScreen?.Show();
            loadingScreen?.SetProgress(0f);

            var startTime = Time.realtimeSinceStartup;
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"Could not start loading scene '{sceneName}'. Is it in Build Settings?");
                loadingScreen?.Hide();
                isLoading = false;
                yield break;
            }

            while (!operation.isDone)
            {
                loadingScreen?.SetProgress(Mathf.Clamp01(operation.progress / 0.9f));
                yield return null;
            }

            var remaining = minimumVisibleDuration - (Time.realtimeSinceStartup - startTime);
            while (remaining > 0f)
            {
                loadingScreen?.SetProgress(1f);
                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            InitializeActiveScene();
            loadingScreen?.Hide();
            isLoading = false;
        }

        private void InitializeActiveScene()
        {
            if (context == null)
            {
                Debug.LogError("Scene flow has no ApplicationContext to inject.");
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var entryPoint = roots[i].GetComponentInChildren<SceneEntryPoint>(true);
                if (entryPoint == null)
                {
                    continue;
                }

                entryPoint.Initialize(context);
                return;
            }

            Debug.LogError($"Scene '{activeScene.name}' has no SceneEntryPoint.");
        }
    }
}
