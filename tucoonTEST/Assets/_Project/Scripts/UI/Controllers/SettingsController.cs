using System;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Core;

namespace SupermarketTycoon.UI
{
    public sealed class SettingsController : IDisposable
    {
        private readonly SettingsView view;
        private readonly ApplicationContext context;
        private readonly Action resetProgress;
        private Action closeAction;

        public SettingsController(
            SettingsView view,
            ApplicationContext context,
            Action resetProgress)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.resetProgress = resetProgress;

            view.MasterVolume.onValueChanged.AddListener(OnMasterChanged);
            view.MusicVolume.onValueChanged.AddListener(OnMusicChanged);
            view.SfxVolume.onValueChanged.AddListener(OnSfxChanged);
            view.Fullscreen.onValueChanged.AddListener(OnFullscreenChanged);
            view.BackButton.onClick.AddListener(Close);
            view.ResetButton.onClick.AddListener(OnResetRequested);
            view.ConfirmResetButton.onClick.AddListener(OnResetConfirmed);
            view.CancelResetButton.onClick.AddListener(OnResetCancelled);
            view.SetVisible(false);
        }

        public void Open(Action onClosed)
        {
            closeAction = onClosed;
            var data = context.Settings.Data;
            view.SetValues(data.MasterVolume, data.MusicVolume, data.SfxVolume, data.Fullscreen);
            view.SetVisible(true);
            context.Audio.Play(GameSound.UiClick);
        }

        public void Dispose()
        {
            view.MasterVolume.onValueChanged.RemoveListener(OnMasterChanged);
            view.MusicVolume.onValueChanged.RemoveListener(OnMusicChanged);
            view.SfxVolume.onValueChanged.RemoveListener(OnSfxChanged);
            view.Fullscreen.onValueChanged.RemoveListener(OnFullscreenChanged);
            view.BackButton.onClick.RemoveListener(Close);
            view.ResetButton.onClick.RemoveListener(OnResetRequested);
            view.ConfirmResetButton.onClick.RemoveListener(OnResetConfirmed);
            view.CancelResetButton.onClick.RemoveListener(OnResetCancelled);
        }

        private void Close()
        {
            view.SetVisible(false);
            context.Audio.Play(GameSound.UiClick);
            var callback = closeAction;
            closeAction = null;
            callback?.Invoke();
        }

        private void OnMasterChanged(float value)
        {
            context.Settings.SetMasterVolume(value);
        }

        private void OnMusicChanged(float value)
        {
            context.Settings.SetMusicVolume(value);
        }

        private void OnSfxChanged(float value)
        {
            context.Settings.SetSfxVolume(value);
        }

        private void OnFullscreenChanged(bool value)
        {
            context.Settings.SetFullscreen(value);
        }

        private void OnResetRequested()
        {
            context.Audio.Play(GameSound.UiClick);
            view.ShowResetConfirmation(true);
        }

        private void OnResetCancelled()
        {
            context.Audio.Play(GameSound.UiClick);
            view.ShowResetConfirmation(false);
        }

        private void OnResetConfirmed()
        {
            context.Audio.Play(GameSound.UiClick);
            view.ShowResetConfirmation(false);
            view.SetVisible(false);
            closeAction = null;
            resetProgress?.Invoke();
        }
    }
}
