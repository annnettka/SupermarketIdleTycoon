using System;
using System.IO;
using UnityEngine;

namespace SupermarketTycoon.Save
{
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
                var data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(settingsPath));
                if (data == null)
                {
                    return new SettingsData { Fullscreen = Screen.fullScreen };
                }

                data.MasterVolume = Mathf.Clamp01(data.MasterVolume);
                data.MusicVolume = Mathf.Clamp01(data.MusicVolume);
                data.SfxVolume = Mathf.Clamp01(data.SfxVolume);
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
    }
}
