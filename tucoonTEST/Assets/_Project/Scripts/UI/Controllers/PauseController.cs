using System;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Core;
using SupermarketTycoon.Save;

namespace SupermarketTycoon.UI
{
    public sealed class PauseController : IDisposable
    {
        private readonly PauseMenuView view;
        private readonly ApplicationContext context;
        private readonly PauseService pause;
        private readonly GameSaveCoordinator save;
        private readonly SettingsController settings;

        public PauseController(
            PauseMenuView view,
            ApplicationContext context,
            PauseService pause,
            GameSaveCoordinator save,
            SettingsController settings)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.pause = pause ?? throw new ArgumentNullException(nameof(pause));
            this.save = save ?? throw new ArgumentNullException(nameof(save));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            view.ResumeButton.onClick.AddListener(Resume);
            view.SettingsButton.onClick.AddListener(OpenSettings);
            view.MainMenuButton.onClick.AddListener(GoToMainMenu);
            view.SetVisible(false);
        }

        public void Open()
        {
            pause.SetPaused(true);
            view.SetVisible(true);
        }

        public void Dispose()
        {
            view.ResumeButton.onClick.RemoveListener(Resume);
            view.SettingsButton.onClick.RemoveListener(OpenSettings);
            view.MainMenuButton.onClick.RemoveListener(GoToMainMenu);
        }

        private void Resume()
        {
            context.Audio.Play(GameSound.UiClick);
            view.SetVisible(false);
            pause.SetPaused(false);
        }

        private void OpenSettings()
        {
            view.SetVisible(false);
            settings.Open(() => view.SetVisible(true));
        }

        private void GoToMainMenu()
        {
            context.Audio.Play(GameSound.UiClick);
            save.SaveNow();
            pause.SetPaused(false);
            context.SceneFlow.LoadMainMenu();
        }
    }
}
