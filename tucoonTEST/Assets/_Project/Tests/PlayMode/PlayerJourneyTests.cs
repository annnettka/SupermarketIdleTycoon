using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SupermarketTycoon.Bootstrap;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Customers;
using SupermarketTycoon.Expansion;
using SupermarketTycoon.Products;
using SupermarketTycoon.Save;
using SupermarketTycoon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SupermarketTycoon.Tests
{
    /// <summary>
    /// Verifies the production Bootstrap-to-Menu-to-Game journey and restores any pre-existing local user data.
    /// Проверяет производственный путь Bootstrap-Menu-Game и восстанавливает существовавшие локальные данные пользователя.
    /// </summary>
    public sealed class PlayerJourneyTests
    {
        private static readonly string[] PersistentFileNames =
        {
            "supermarket-save.json",
            "supermarket-save.backup.json",
            "supermarket-save.json.tmp",
            "supermarket-settings.json"
        };

        [UnityTest]
        public IEnumerator FullJourney_BuildsEarnsSavesContinuesAndResets()
        {
            var originalFiles = CapturePersistentFiles();
            var savePath = Path.Combine(Application.persistentDataPath, "supermarket-save.json");
            try
            {
                DeletePersistentFiles();
                SeedAdvancedSave(savePath);

                SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
                yield return WaitForScene("MainMenu", 12f);

                var menu = Find<MainMenuView>();
                var menuSettings = Find<SettingsView>();
                yield return WaitForMusicClip("vintage_menu", 4f);

                menu.SettingsButton.onClick.Invoke();
                yield return null;
                menuSettings.MasterVolume.value = 0.65f;
                menuSettings.MusicVolume.value = 0.6f;
                menuSettings.SfxVolume.value = 0.7f;
                yield return null;
                Assert.That(File.Exists(Path.Combine(
                    Application.persistentDataPath,
                    "supermarket-settings.json")), Is.True);
                menuSettings.BackButton.onClick.Invoke();

                menu.PlayButton.onClick.Invoke();
                yield return WaitForScene("Game", 12f);
                yield return WaitForMusicClip("Two Left Socks", 4f);

                var spots = Object.FindObjectsByType<BuildSpot>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var shelf = spots.Single(spot => spot.StableId == "spot.shelf.a");
                var checkout = spots.Single(spot => spot.StableId == "spot.checkout.a");
                ClickBuildChip(shelf);
                ClickBuildChip(checkout);
                yield return null;
                Assert.That(shelf.IsBuilt, Is.True);
                Assert.That(checkout.IsBuilt, Is.True);

                ClickBuildChip(shelf);
                yield return null;
                var buildingPanel = Find<BuildingPanelView>();
                Assert.That(buildingPanel.UpgradeButton.interactable, Is.True);
                buildingPanel.UpgradeButton.onClick.Invoke();
                yield return null;
                Assert.That(shelf.CurrentLevel, Is.EqualTo(2));
                buildingPanel.CloseButton.onClick.Invoke();

                var expansion = Find<StoreExpansionSpot>();
                var expansionButton = expansion.GetComponentInChildren<Button>(true);
                Assert.That(expansionButton.interactable, Is.True);
                expansionButton.onClick.Invoke();
                yield return null;
                Assert.That(expansion.IsPurchased, Is.True);

                var gatedSpots = spots.Where(spot => !string.IsNullOrEmpty(spot.RequiredExpansionId)).ToArray();
                for (var i = 0; i < gatedSpots.Length; i++)
                {
                    ClickBuildChip(gatedSpots[i]);
                }

                yield return null;
                Assert.That(gatedSpots.All(spot => spot.IsBuilt), Is.True);
                Assert.That(GameObject.Find("Fresh Food Island"), Is.Not.Null);
                Assert.That(spots.Single(spot => spot.StableId == "spot.shelf.premium").IsBuilt, Is.True);

                var employee = Find<EmployeeView>();
                Assert.That(employee.PurchaseButton.interactable, Is.True);
                employee.PurchaseButton.onClick.Invoke();
                yield return null;

                var spawner = Find<CustomerSpawner>();
                var stock = shelf.BuildingObject.GetComponent<ShelfStation>().StockView;
                var initialStock = stock.AvailableCount;
                var paymentCompleted = false;
                spawner.PaymentCompleted += (_, _) => paymentCompleted = true;
                var sawStockConsumption = false;
                var paymentDeadline = Time.realtimeSinceStartup + 50f;
                while (!paymentCompleted && Time.realtimeSinceStartup < paymentDeadline)
                {
                    sawStockConsumption |= stock.AvailableCount < initialStock;
                    yield return null;
                }

                Assert.That(sawStockConsumption, Is.True, "Customer never consumed logical shelf stock.");
                Assert.That(paymentCompleted, Is.True, "Customer never completed checkout payment.");

                spawner.enabled = false;
                var restockDeadline = Time.realtimeSinceStartup + 30f;
                while ((stock.AvailableCount < initialStock ||
                        Object.FindObjectsByType<CustomerAgent>(
                            FindObjectsInactive.Exclude,
                            FindObjectsSortMode.None).Length > 0) &&
                       Time.realtimeSinceStartup < restockDeadline)
                {
                    yield return null;
                }

                Assert.That(stock.AvailableCount, Is.EqualTo(initialStock),
                    "Consumed logical shelf stock was not restored.");

                var hud = Find<GameHudView>();
                var pause = Find<PauseMenuView>();
                hud.PauseButton.onClick.Invoke();
                yield return null;
                Assert.That(Time.timeScale, Is.Zero);
                pause.ResumeButton.onClick.Invoke();
                yield return null;
                Assert.That(Time.timeScale, Is.EqualTo(1f));

                hud.PauseButton.onClick.Invoke();
                yield return null;
                pause.MainMenuButton.onClick.Invoke();
                yield return WaitForScene("MainMenu", 12f);

                var persisted = JsonUtility.FromJson<SaveData>(File.ReadAllText(savePath));
                Assert.That(persisted.BuiltBuildings.Count, Is.GreaterThanOrEqualTo(5));
                Assert.That(persisted.BuiltBuildings.Single(
                    building => building.BuildSpotId == "spot.shelf.a").BuildingLevel, Is.EqualTo(2));
                Assert.That(persisted.PurchasedExpansionIds, Does.Contain("expansion.main"));
                Assert.That(persisted.CashierLevel, Is.GreaterThanOrEqualTo(1));
                Assert.That(persisted.CurrentLevel, Is.EqualTo(5));
                Assert.That(persisted.CurrentObjectiveIndex, Is.GreaterThanOrEqualTo(1));
                Assert.That(persisted.LifetimeStats.CustomersServed, Is.GreaterThanOrEqualTo(1));

                Find<MainMenuView>().PlayButton.onClick.Invoke();
                yield return WaitForScene("Game", 12f);
                var restoredShelf = Object.FindObjectsByType<BuildSpot>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Single(spot => spot.StableId == "spot.shelf.a");
                Assert.That(restoredShelf.IsBuilt, Is.True);
                Assert.That(restoredShelf.CurrentLevel, Is.EqualTo(2));

                Find<GameHudView>().PauseButton.onClick.Invoke();
                yield return null;
                var restoredPause = Find<PauseMenuView>();
                restoredPause.SettingsButton.onClick.Invoke();
                yield return null;
                var gameSettings = Find<SettingsView>();
                gameSettings.ResetButton.onClick.Invoke();
                yield return null;
                gameSettings.ConfirmResetButton.onClick.Invoke();
                yield return WaitForScene("MainMenu", 12f);
                Assert.That(File.Exists(savePath), Is.False);
            }
            finally
            {
                Time.timeScale = 1f;
                var bootstrapper = Object.FindFirstObjectByType<AppBootstrapper>(
                    FindObjectsInactive.Include);
                if (bootstrapper != null)
                {
                    Object.Destroy(bootstrapper.gameObject);
                }

                RestorePersistentFiles(originalFiles);
            }
        }

        private static T Find<T>() where T : Object
        {
            var value = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            Assert.That(value, Is.Not.Null, typeof(T).Name);
            return value;
        }

        private static void ClickBuildChip(BuildSpot spot)
        {
            var button = spot.GetComponentInChildren<Button>(true);
            Assert.That(button, Is.Not.Null, spot.StableId);
            Assert.That(button.interactable, Is.True, spot.StableId);
            button.onClick.Invoke();
        }

        private static IEnumerator WaitForMusicClip(string expectedName, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                var routed = Object.FindObjectsByType<AudioSource>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Any(source => source.clip != null &&
                                   source.clip.name == expectedName &&
                                   source.loop &&
                                   source.gameObject.activeInHierarchy);
                if (routed)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Music clip '{expectedName}' was not routed to an active looping AudioSource.");
        }

        private static IEnumerator WaitForScene(string sceneName, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (SceneManager.GetActiveScene().name != sceneName &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
            yield return new WaitForSecondsRealtime(1f);
        }

        private static Dictionary<string, byte[]> CapturePersistentFiles()
        {
            var files = new Dictionary<string, byte[]>();
            for (var i = 0; i < PersistentFileNames.Length; i++)
            {
                var path = Path.Combine(Application.persistentDataPath, PersistentFileNames[i]);
                if (File.Exists(path))
                {
                    files.Add(path, File.ReadAllBytes(path));
                }
            }

            return files;
        }

        private static void DeletePersistentFiles()
        {
            for (var i = 0; i < PersistentFileNames.Length; i++)
            {
                var path = Path.Combine(Application.persistentDataPath, PersistentFileNames[i]);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static void RestorePersistentFiles(Dictionary<string, byte[]> originalFiles)
        {
            DeletePersistentFiles();
            foreach (var pair in originalFiles)
            {
                File.WriteAllBytes(pair.Key, pair.Value);
            }
        }

        private static void SeedAdvancedSave(string savePath)
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            var data = new SaveData
            {
                SaveVersion = SaveData.CurrentVersion,
                Money = 10000,
                CurrentLevel = 4,
                CurrentXp = 440,
                StoreRating = 3f,
                LastSaveUtcTicks = System.DateTime.UtcNow.Ticks
            };
            File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
        }
    }
}
