using System;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Save;
using UnityEngine;

namespace SupermarketTycoon.Core
{
    /// <summary>
    /// Owns user settings, applies them to Unity and audio adapters, and persists each accepted change.
    /// Владеет пользовательскими настройками, применяет их к Unity и аудио-адаптерам и сохраняет каждое принятое изменение.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly SettingsRepository repository;
        private readonly AudioService audio;

        public SettingsService(SettingsRepository repository, AudioService audio)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.audio = audio ?? throw new ArgumentNullException(nameof(audio));
            Data = repository.Load();
            Apply();
        }

        public SettingsData Data { get; }
        public event Action Changed;

        public void SetMasterVolume(float value)
        {
            Data.MasterVolume = Mathf.Clamp01(value);
            SaveAndApply();
        }

        public void SetMusicVolume(float value)
        {
            Data.MusicVolume = Mathf.Clamp01(value);
            SaveAndApply();
        }

        public void SetSfxVolume(float value)
        {
            Data.SfxVolume = Mathf.Clamp01(value);
            SaveAndApply();
        }

        public void SetFullscreen(bool value)
        {
            Data.Fullscreen = value;
            Screen.fullScreen = value;
            repository.Save(Data);
            Changed?.Invoke();
        }

        private void SaveAndApply()
        {
            repository.Save(Data);
            Apply();
            Changed?.Invoke();
        }

        private void Apply()
        {
            Screen.fullScreen = Data.Fullscreen;
            audio.Apply(Data);
        }
    }
}
