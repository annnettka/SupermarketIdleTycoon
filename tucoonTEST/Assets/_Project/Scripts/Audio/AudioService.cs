using System;
using SupermarketTycoon.Save;
using UnityEngine;

namespace SupermarketTycoon.Audio
{
    public enum GameSound
    {
        UiClick,
        Build,
        Income,
        LevelUp
    }

    public sealed class AudioService
    {
        private readonly AudioSource musicSource;
        private readonly AudioSource sfxSource;
        private readonly AudioClip uiClick;
        private readonly AudioClip build;
        private readonly AudioClip income;
        private readonly AudioClip levelUp;

        public AudioService(
            AudioSource musicSource,
            AudioSource sfxSource,
            AudioClip uiClick,
            AudioClip build,
            AudioClip income,
            AudioClip levelUp)
        {
            this.musicSource = musicSource;
            this.sfxSource = sfxSource;
            this.uiClick = uiClick;
            this.build = build;
            this.income = income;
            this.levelUp = levelUp;
        }

        public void Apply(SettingsData settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            AudioListener.volume = settings.MasterVolume;
            if (musicSource != null)
            {
                musicSource.volume = settings.MusicVolume;
            }

            if (sfxSource != null)
            {
                sfxSource.volume = settings.SfxVolume;
            }
        }

        public void Play(GameSound sound)
        {
            if (sfxSource == null)
            {
                return;
            }

            var clip = sound switch
            {
                GameSound.UiClick => uiClick,
                GameSound.Build => build,
                GameSound.Income => income,
                GameSound.LevelUp => levelUp,
                _ => null
            };

            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
    }
}
