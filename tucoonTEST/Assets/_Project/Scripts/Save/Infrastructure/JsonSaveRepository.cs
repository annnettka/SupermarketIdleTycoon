using System;
using System.Collections.Generic;
using System.IO;
using SupermarketTycoon.Core;
using UnityEngine;

namespace SupermarketTycoon.Save
{
    public interface ISaveRepository
    {
        bool HasSave { get; }
        SaveData LoadOrCreate(GameConfig config);
        void Save(SaveData data);
        void Delete();
    }

    public sealed class JsonSaveRepository : ISaveRepository
    {
        private const string FileName = "supermarket-save.json";
        private const string BackupFileName = "supermarket-save.backup.json";

        private readonly string savePath;
        private readonly string backupPath;

        public JsonSaveRepository(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("A save directory is required.", nameof(directoryPath));
            }

            savePath = Path.Combine(directoryPath, FileName);
            backupPath = Path.Combine(directoryPath, BackupFileName);
        }

        public bool HasSave => File.Exists(savePath);

        public SaveData LoadOrCreate(GameConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!File.Exists(savePath))
            {
                return CreateFresh(config);
            }

            try
            {
                var json = File.ReadAllText(savePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || data.SaveVersion <= 0)
                {
                    throw new InvalidDataException("Save data is empty or has no supported version.");
                }

                MigrateAndNormalize(data);
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load gameplay save. A fresh game will be used. {exception.Message}");
                TryBackupCorruptedFile();
                return CreateFresh(config);
            }
        }

        public void Save(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                MigrateAndNormalize(data);
                var directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var temporaryPath = savePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
                if (File.Exists(savePath))
                {
                    File.Copy(savePath, backupPath, true);
                }

                File.Copy(temporaryPath, savePath, true);
                File.Delete(temporaryPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to save gameplay progress: {exception.Message}");
            }
        }

        public void Delete()
        {
            TryDelete(savePath);
            TryDelete(backupPath);
        }

        private static SaveData CreateFresh(GameConfig config)
        {
            return new SaveData
            {
                SaveVersion = SaveData.CurrentVersion,
                Money = config.StartingMoney,
                CurrentLevel = 1,
                CurrentXp = 0,
                StoreRating = 3f,
                LastSaveUtcTicks = DateTime.UtcNow.Ticks
            };
        }

        private static void MigrateAndNormalize(SaveData data)
        {
            data.Money = Math.Max(0, data.Money);
            data.CurrentLevel = Math.Max(1, data.CurrentLevel);
            data.CurrentXp = Math.Max(0, data.CurrentXp);
            data.BuiltBuildings ??= new List<BuiltBuildingData>();
            data.PurchasedExpansionIds ??= new List<string>();
            data.LifetimeStats ??= new LifetimeStatsData();
            data.CashierLevel = Math.Max(0, data.CashierLevel);
            data.StoreRating = data.StoreRating <= 0f
                ? 3f
                : Mathf.Clamp(data.StoreRating, 1f, 5f);
            data.CurrentObjectiveIndex = Math.Max(0, data.CurrentObjectiveIndex);
            data.CurrentObjectiveProgress = Math.Max(0, data.CurrentObjectiveProgress);
            data.PendingOfflineIncome = Math.Max(0, data.PendingOfflineIncome);

            for (var i = data.BuiltBuildings.Count - 1; i >= 0; i--)
            {
                var building = data.BuiltBuildings[i];
                if (building == null || string.IsNullOrWhiteSpace(building.BuildSpotId))
                {
                    data.BuiltBuildings.RemoveAt(i);
                    continue;
                }

                building.BuildingLevel = Math.Max(1, building.BuildingLevel);
            }

            data.SaveVersion = SaveData.CurrentVersion;
        }

        private void TryBackupCorruptedFile()
        {
            try
            {
                File.Copy(savePath, backupPath, true);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not back up the corrupted save: {exception.Message}");
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to delete save file '{path}': {exception.Message}");
            }
        }
    }
}
