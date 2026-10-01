using System;
using System.IO;
using UnityEngine;

namespace SupermarketTycoon.Save
{
    /// <summary>
    /// Persists user preferences separately from resettable gameplay progress and supplies safe defaults on failure.
    /// Хранит пользовательские настройки отдельно от сбрасываемого игрового прогресса и возвращает безопасные значения при ошибке.
    /// </summary>
    public sealed class SettingsRepository
    {
        private readonly string settingsPath;

        public SettingsRepository(string directoryPath)
        {
            settingsPath = Path.Combine(directoryPath, "supermarket-settings.json");
        }

        public SettingsData Load()
        {
            if (!File.Exists(settingsPath))
            {
                return new SettingsData { Fullscreen = Screen.fullScreen };
            }

            try
            {
                var data = new SettingsData { Fullscreen = Screen.fullScreen };
                JsonUtility.FromJsonOverwrite(File.ReadAllText(settingsPath), data);

                data.MasterVolume = NormalizeVolume(data.MasterVolume, 1f);
                data.MusicVolume = NormalizeVolume(data.MusicVolume, 0.75f);
                data.SfxVolume = NormalizeVolume(data.SfxVolume, 1f);
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load settings. Defaults will be used. {exception.Message}");
                return new SettingsData { Fullscreen = Screen.fullScreen };
            }
        }

        public void Save(SettingsData data)
        {
            try
            {
                var directory = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(settingsPath, JsonUtility.ToJson(data, true));
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to save settings: {exception.Message}");
            }
        }

        private static float NormalizeVolume(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Clamp01(value);
        }
    }
}
