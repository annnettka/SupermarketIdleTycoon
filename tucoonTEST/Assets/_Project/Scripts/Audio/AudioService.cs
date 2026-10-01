using System;
using System.Collections;
using System.Collections.Generic;
using SupermarketTycoon.Save;
using UnityEngine;
using UnityEngine.Audio;

namespace SupermarketTycoon.Audio
{
    /// <summary>
    /// Identifies semantic sound effects so gameplay code is independent of concrete audio clips.
    /// Обозначает смысловые звуковые эффекты, чтобы игровой код не зависел от конкретных аудиоклипов.
    /// </summary>
    public enum GameSound
    {
        UiClick,
        Build,
        Income,
        LevelUp
    }

    /// <summary>
    /// Owns music transitions, effect playback, and mixer volume mapping for the application lifetime.
    /// Владеет переходами музыки, воспроизведением эффектов и настройкой громкости микшера на время жизни приложения.
    /// </summary>
    public sealed class AudioService
    {
        private const string MasterVolumeParameter = "MasterVolume";
        private const string MusicVolumeParameter = "MusicVolume";
        private const string SfxVolumeParameter = "SfxVolume";
        private const float MutedDb = -80f;
        private const float MusicFadeDuration = 0.7f;

        private readonly MonoBehaviour coroutineHost;
        private readonly AudioMixer mixer;
        private readonly AudioSource primaryMusicSource;
        private readonly AudioSource secondaryMusicSource;
        private readonly AudioSource sfxSource;
        private readonly AudioClip menuMusic;
        private readonly AudioClip gameplayMusic;
        private readonly AudioClip uiClick;
        private readonly AudioClip build;
        private readonly AudioClip income;
        private readonly AudioClip levelUp;
        private readonly HashSet<string> invalidMixerParameters = new();

        private AudioSource activeMusicSource;
        private AudioClip requestedMusic;
        private Coroutine musicTransition;
        private float masterVolume = 1f;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;
        private float primaryMusicGain;
        private float secondaryMusicGain;
        private bool mixerMasterAvailable;
        private bool mixerMusicAvailable;
        private bool mixerSfxAvailable;

        public AudioService(
            MonoBehaviour coroutineOwner,
            AudioMixer audioMixer,
            AudioSource firstMusicSource,
            AudioSource secondMusicSource,
            AudioSource sfxSource,
            AudioClip menuMusicClip,
            AudioClip gameplayMusicClip,
            AudioClip uiClick,
            AudioClip build,
            AudioClip income,
            AudioClip levelUp)
        {
            coroutineHost = coroutineOwner;
            mixer = audioMixer;
            primaryMusicSource = firstMusicSource;
            secondaryMusicSource = secondMusicSource;
            this.sfxSource = sfxSource;
            menuMusic = menuMusicClip;
            gameplayMusic = gameplayMusicClip;
            this.uiClick = uiClick;
            this.build = build;
            this.income = income;
            this.levelUp = levelUp;

            ConfigureMusicSource(primaryMusicSource);
            ConfigureMusicSource(secondaryMusicSource);
            AudioListener.pause = false;
            if (this.sfxSource != null)
            {
                this.sfxSource.playOnAwake = false;
            }
        }

        public void Apply(SettingsData settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            SetMasterVolume(settings.MasterVolume);
            SetMusicVolume(settings.MusicVolume);
            SetSfxVolume(settings.SfxVolume);
            AudioListener.pause = false;
        }

        public void PlayMenuMusic()
        {
            PlayMusic(menuMusic);
        }

        public void PlayGameplayMusic()
        {
            PlayMusic(gameplayMusic);
        }

        public void StopMusic()
        {
            TransitionTo(null);
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            mixerMasterAvailable = SetMixerVolume(MasterVolumeParameter, masterVolume);
            AudioListener.volume = mixerMasterAvailable ? 1f : masterVolume;
            ApplyFallbackVolumes();
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            mixerMusicAvailable = SetMixerVolume(MusicVolumeParameter, musicVolume);
            ApplyFallbackVolumes();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            mixerSfxAvailable = SetMixerVolume(SfxVolumeParameter, sfxVolume);
            ApplyFallbackVolumes();
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

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("Requested music clip is not assigned.");
                return;
            }

            if (primaryMusicSource == null || secondaryMusicSource == null)
            {
                Debug.LogError("Music playback requires two configured AudioSources.");
                return;
            }

            if (requestedMusic == clip && activeMusicSource != null &&
                activeMusicSource.clip == clip && activeMusicSource.isPlaying)
            {
                return;
            }

            TransitionTo(clip);
        }

        private void TransitionTo(AudioClip clip)
        {
            requestedMusic = clip;
            if (coroutineHost == null || primaryMusicSource == null || secondaryMusicSource == null)
            {
                PlayImmediately(clip);
                return;
            }

            if (musicTransition != null)
            {
                coroutineHost.StopCoroutine(musicTransition);
            }

            musicTransition = coroutineHost.StartCoroutine(CrossfadeMusic(clip));
        }

        private IEnumerator CrossfadeMusic(AudioClip clip)
        {
            var outgoing = activeMusicSource;
            var incoming = GetOtherMusicSource(outgoing);
            var outgoingStart = GetMusicGain(outgoing);

            if (clip != null)
            {
                incoming.Stop();
                incoming.clip = clip;
                incoming.loop = true;
                SetMusicGain(incoming, 0f);
                incoming.Play();
            }

            var elapsed = 0f;
            while (elapsed < MusicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / MusicFadeDuration);
                SetMusicGain(outgoing, Mathf.Lerp(outgoingStart, 0f, progress));
                if (clip != null)
                {
                    SetMusicGain(incoming, progress);
                }

                yield return null;
            }

            if (outgoing != null)
            {
                outgoing.Stop();
                outgoing.clip = null;
                SetMusicGain(outgoing, 0f);
            }

            activeMusicSource = clip != null ? incoming : null;
            if (activeMusicSource != null)
            {
                SetMusicGain(activeMusicSource, 1f);
                if (!activeMusicSource.isPlaying)
                {
                    Debug.LogError($"Music source failed to play '{activeMusicSource.clip?.name ?? "<missing>"}'.");
                }
            }

            musicTransition = null;
        }

        private void PlayImmediately(AudioClip clip)
        {
            if (primaryMusicSource == null)
            {
                return;
            }

            primaryMusicSource.Stop();
            primaryMusicSource.clip = clip;
            if (clip == null)
            {
                activeMusicSource = null;
                SetMusicGain(primaryMusicSource, 0f);
                return;
            }

            primaryMusicSource.loop = true;
            activeMusicSource = primaryMusicSource;
            SetMusicGain(primaryMusicSource, 1f);
            primaryMusicSource.Play();
        }

        private AudioSource GetOtherMusicSource(AudioSource source)
        {
            return source == primaryMusicSource ? secondaryMusicSource : primaryMusicSource;
        }

        private void ApplyFallbackVolumes()
        {
            ApplyMusicSourceVolume(primaryMusicSource, primaryMusicGain);
            ApplyMusicSourceVolume(secondaryMusicSource, secondaryMusicGain);
            if (sfxSource != null)
            {
                var masterGain = mixerMasterAvailable ? 1f : masterVolume;
                var groupGain = mixerSfxAvailable ? 1f : sfxVolume;
                sfxSource.volume = masterGain * groupGain;
            }
        }

        private void SetMusicGain(AudioSource source, float gain)
        {
            if (source == null)
            {
                return;
            }

            gain = Mathf.Clamp01(gain);
            if (source == primaryMusicSource)
            {
                primaryMusicGain = gain;
            }
            else if (source == secondaryMusicSource)
            {
                secondaryMusicGain = gain;
            }

            ApplyMusicSourceVolume(source, gain);
        }

        private float GetMusicGain(AudioSource source)
        {
            if (source == primaryMusicSource)
            {
                return primaryMusicGain;
            }

            return source == secondaryMusicSource ? secondaryMusicGain : 0f;
        }

        private void ApplyMusicSourceVolume(AudioSource source, float transitionGain)
        {
            if (source == null)
            {
                return;
            }

            var masterGain = mixerMasterAvailable ? 1f : masterVolume;
            var groupGain = mixerMusicAvailable ? 1f : musicVolume;
            source.volume = transitionGain * masterGain * groupGain;
        }

        private bool SetMixerVolume(string parameter, float normalizedValue)
        {
            if (mixer == null)
            {
                return false;
            }

            var applied = mixer.SetFloat(parameter, ToDecibels(normalizedValue));
            if (!applied && invalidMixerParameters.Add(parameter))
            {
                Debug.LogError($"AudioMixer parameter '{parameter}' is missing or not exposed.");
            }

            return applied;
        }

        private static float ToDecibels(float normalizedValue)
        {
            return normalizedValue <= 0.0001f
                ? MutedDb
                : Mathf.Log10(Mathf.Clamp01(normalizedValue)) * 20f;
        }

        private static void ConfigureMusicSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.enabled = true;
            source.loop = true;
            source.mute = false;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.volume = 0f;
        }
    }
}
