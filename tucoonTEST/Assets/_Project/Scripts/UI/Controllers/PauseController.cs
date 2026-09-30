using System;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Core;
using SupermarketTycoon.Save;
using SupermarketTycoon.Stats;

namespace SupermarketTycoon.UI
{
    public sealed class PauseController : IDisposable
    {
        private readonly PauseMenuView view;
        private readonly StatsView statsView;
        private readonly StatisticsService stats;
        private readonly ApplicationContext context;
        private readonly PauseService pause;
        private readonly GameSaveCoordinator save;
        private readonly SettingsController settings;

        public PauseController(
            PauseMenuView view,
            StatsView statsView,
            StatisticsService stats,
            ApplicationContext context,
            PauseService pause,
            GameSaveCoordinator save,
            SettingsController settings)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.statsView = statsView ?? throw new ArgumentNullException(nameof(statsView));
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.pause = pause ?? throw new ArgumentNullException(nameof(pause));
            this.save = save ?? throw new ArgumentNullException(nameof(save));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            view.ResumeButton.onClick.AddListener(Resume);
            view.StatsButton.onClick.AddListener(OpenStats);
            view.SettingsButton.onClick.AddListener(OpenSettings);
            view.MainMenuButton.onClick.AddListener(GoToMainMenu);
            statsView.BackButton.onClick.AddListener(CloseStats);
            view.SetVisible(false);
            statsView.Hide();
        }

        public void Open()
        {
            pause.SetPaused(true);
            view.SetVisible(true);
        }

        public void Dispose()
        {
            view.ResumeButton.onClick.RemoveListener(Resume);
            view.StatsButton.onClick.RemoveListener(OpenStats);
            view.SettingsButton.onClick.RemoveListener(OpenSettings);
            view.MainMenuButton.onClick.RemoveListener(GoToMainMenu);
            statsView.BackButton.onClick.RemoveListener(CloseStats);
        }

        private void Resume()
        {
            context.Audio.Play(GameSound.UiClick);
            statsView.Hide();
            view.SetVisible(false);
            pause.SetPaused(false);
        }

        private void OpenStats()
        {
            context.Audio.Play(GameSound.UiClick);
            view.SetVisible(false);
            statsView.Show(stats);
        }

        private void CloseStats()
        {
            context.Audio.Play(GameSound.UiClick);
            statsView.Hide();
            view.SetVisible(true);
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
