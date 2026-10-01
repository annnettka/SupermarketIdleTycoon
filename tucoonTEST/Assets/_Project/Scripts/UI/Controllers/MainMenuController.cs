using System;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Core;
using UnityEngine;

namespace SupermarketTycoon.UI
{
    /// <summary>
    /// Binds main-menu commands to scene flow, settings, save availability, audio, and application exit.
    /// Связывает команды главного меню с переходами сцен, настройками, наличием сохранения, аудио и выходом.
    /// </summary>
    public sealed class MainMenuController : IDisposable
    {
        private readonly MainMenuView view;
        private readonly ApplicationContext context;
        private readonly SettingsController settings;

        public MainMenuController(
            MainMenuView view,
            ApplicationContext context,
            SettingsController settings)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            view.PlayButton.onClick.AddListener(Play);
            view.SettingsButton.onClick.AddListener(OpenSettings);
            view.QuitButton.onClick.AddListener(Quit);
            Refresh();
        }

        public void Refresh()
        {
            view.SetVisible(true);
            view.SetHasSave(context.SaveRepository.HasSave);
        }

        public void Dispose()
        {
            view.PlayButton.onClick.RemoveListener(Play);
            view.SettingsButton.onClick.RemoveListener(OpenSettings);
            view.QuitButton.onClick.RemoveListener(Quit);
        }

        private void Play()
        {
            context.Audio.Play(GameSound.UiClick);
            context.SceneFlow.LoadGame();
        }

        private void OpenSettings()
        {
            view.SetVisible(false);
            settings.Open(Refresh);
        }

        private void Quit()
        {
            context.Audio.Play(GameSound.UiClick);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
