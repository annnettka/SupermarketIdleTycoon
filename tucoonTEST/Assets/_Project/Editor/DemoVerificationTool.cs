using System;
using System.Collections.Generic;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Checkout;
using SupermarketTycoon.Customers;
using SupermarketTycoon.Employees;
using SupermarketTycoon.Expansion;
using SupermarketTycoon.Objectives;
using SupermarketTycoon.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SupermarketTycoon.Editor
{
    /// <summary>
    /// Validates generated data, wrappers, scene references, stable IDs, scripts, and render compatibility.
    /// Проверяет созданные данные, оболочки, ссылки сцен, стабильные ID, скрипты и совместимость рендеринга.
    /// </summary>
    public static class DemoVerificationTool
    {
        private const string MenuPath = "Tools/Supermarket Tycoon/Validate Playable Demo";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string DataRoot = "Assets/_Project/Data/ScriptableObjects";
        private const string PrefabRoot = "Assets/_Project/Art/Prefabs";

        /// <summary>
        /// Runs playable-demo validation from the Unity Tools menu.
        /// Запускает проверку игрового демо из меню Tools редактора Unity.
        /// </summary>
        [MenuItem(MenuPath)]
        public static void ValidatePlayableDemo()
        {
            ValidatePlayableDemoBatch();
        }

        /// <summary>
        /// Runs the same validation in batch mode and throws one aggregated diagnostic on failure.
        /// Выполняет ту же проверку в пакетном режиме и при ошибке выбрасывает одну сводную диагностику.
        /// </summary>
        public static void ValidatePlayableDemoBatch()
        {
            var errors = new List<string>();
            ValidateData(errors);
            ValidatePrefabs(errors);
            ValidateScene(errors);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Playable demo validation failed:\n- " + string.Join("\n- ", errors));
            }

            Debug.Log("[DemoVerification] PASS: data, prefabs, scene references, scripts, materials, build spots, and queue points are valid.");
        }

        private static void ValidateData(List<string> errors)
        {
            RequireAsset<CustomerConfig>(DataRoot + "/CustomerConfig.asset", errors);
            RequireAsset<ObjectiveConfig>(DataRoot + "/ObjectiveConfig.asset", errors);
            RequireAsset<EmployeeDefinition>(DataRoot + "/CashierEmployee.asset", errors);
            RequireAsset<StoreExpansionDefinition>(DataRoot + "/StoreExpansion.asset", errors);
            RequireAsset<CustomerProfileDefinition>(DataRoot + "/CustomerNormal.asset", errors);
            RequireAsset<CustomerProfileDefinition>(DataRoot + "/CustomerImpatient.asset", errors);
            RequireAsset<CustomerProfileDefinition>(DataRoot + "/CustomerVip.asset", errors);

            var buildingPaths = new[]
            {
                DataRoot + "/ShelfBuilding.asset",
                DataRoot + "/CheckoutBuilding.asset",
                DataRoot + "/ShelfExpansion.asset",
                DataRoot + "/StoreExpansionShelf.asset",
                DataRoot + "/CheckoutExpansion.asset",
                DataRoot + "/PremiumShelf.asset"
            };
            for (var i = 0; i < buildingPaths.Length; i++)
            {
                var definition = RequireAsset<BuildingDefinition>(buildingPaths[i], errors);
                if (definition != null && definition.MaxLevel < 2)
                {
                    errors.Add($"Building definition has no upgrade path: {buildingPaths[i]}");
                }
            }

            var customerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/Characters/Customer.prefab");
            var visualSelector = customerPrefab != null
                ? customerPrefab.GetComponent<CustomerVisualSelector>()
                : null;
            var variants = visualSelector != null
                ? new SerializedObject(visualSelector).FindProperty("variants")
                : null;
            if (variants == null || variants.arraySize < 5)
            {
                errors.Add("Customer wrapper must configure at least five visual variants.");
            }
            else
            {
                for (var i = 0; i < variants.arraySize; i++)
                {
                    var root = variants.GetArrayElementAtIndex(i).FindPropertyRelative("root");
                    if (root == null || root.objectReferenceValue == null)
                    {
                        errors.Add($"Customer visual variant {i + 1} has no project-owned visual root.");
                    }
                }
            }
        }

        private static void ValidatePrefabs(List<string> errors)
        {
            var checkoutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/Buildings/CheckoutBuilding.prefab");
            if (checkoutPrefab == null)
            {
                errors.Add("Checkout wrapper prefab is missing.");
            }
            else
            {
                var station = checkoutPrefab.GetComponent<CheckoutStation>();
                var serialized = station != null ? new SerializedObject(station) : null;
                var points = serialized?.FindProperty("queuePoints");
                if (points == null || points.arraySize != 5)
                {
                    errors.Add("Checkout wrapper must contain five serialized queue points.");
                }
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
            for (var i = 0; i < prefabGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ValidateRoot(root, "Prefab " + path, errors);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void ValidateScene(List<string> errors)
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            GameSceneEntryPoint entry = null;
            var buildSpots = new List<BuildSpot>();
            var expansionSpots = new List<StoreExpansionSpot>();

            for (var i = 0; i < roots.Length; i++)
            {
                ValidateRoot(roots[i], "Game scene", errors);
                entry ??= roots[i].GetComponentInChildren<GameSceneEntryPoint>(true);
                buildSpots.AddRange(roots[i].GetComponentsInChildren<BuildSpot>(true));
                expansionSpots.AddRange(roots[i].GetComponentsInChildren<StoreExpansionSpot>(true));
            }

            if (entry == null)
            {
                errors.Add("GameSceneEntryPoint is missing.");
                return;
            }

            if (buildSpots.Count != 6)
            {
                errors.Add($"Expected six BuildSpots, found {buildSpots.Count}.");
            }

            if (expansionSpots.Count != 1)
            {
                errors.Add($"Expected one StoreExpansionSpot, found {expansionSpots.Count}.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < buildSpots.Count; i++)
            {
                if (!ids.Add(buildSpots[i].StableId))
                {
                    errors.Add($"Duplicate BuildSpot ID: {buildSpots[i].StableId}");
                }
            }

            var entryData = new SerializedObject(entry);
            RequireReference(entryData, "gameConfig", errors);
            RequireReference(entryData, "customerConfig", errors);
            RequireReference(entryData, "progressionConfig", errors);
            RequireReference(entryData, "objectiveConfig", errors);
            RequireReference(entryData, "employeeDefinition", errors);
            RequireReference(entryData, "customerSpawner", errors);
            RequireReference(entryData, "hudView", errors);
            RequireReference(entryData, "buildingPanelView", errors);
            RequireReference(entryData, "employeeView", errors);
            RequireReference(entryData, "offlineIncomeView", errors);
            RequireReference(entryData, "pauseView", errors);
            RequireReference(entryData, "statsView", errors);
            RequireReference(entryData, "settingsView", errors);
            RequireArray(entryData, "customerProfiles", 3, errors);
            RequireArray(entryData, "buildSpots", 6, errors);
            RequireArray(entryData, "expansionSpots", 1, errors);
        }

        private static T RequireAsset<T>(string path, List<string> errors) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                errors.Add($"Missing {typeof(T).Name}: {path}");
            }

            return asset;
        }

        private static void RequireReference(SerializedObject serialized, string propertyName, List<string> errors)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                errors.Add($"GameSceneEntryPoint reference is missing: {propertyName}");
            }
        }

        private static void RequireArray(
            SerializedObject serialized,
            string propertyName,
            int expectedSize,
            List<string> errors)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null || !property.isArray || property.arraySize != expectedSize)
            {
                errors.Add($"GameSceneEntryPoint array '{propertyName}' must contain {expectedSize} entries.");
            }
        }

        private static void ValidateRoot(GameObject root, string owner, List<string> errors)
        {
            var missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
            if (missingScripts > 0)
            {
                errors.Add($"{owner} contains {missingScripts} missing script reference(s) at {root.name}.");
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var materials = renderers[i].sharedMaterials;
                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    var material = materials[materialIndex];
                    if (material == null || material.shader == null || !material.shader.isSupported)
                    {
                        errors.Add($"{owner} has a missing or unsupported material on {renderers[i].name}.");
                        continue;
                    }

                    if (GraphicsSettings.currentRenderPipeline != null &&
                        string.Equals(material.shader.name, "Standard", StringComparison.Ordinal))
                    {
                        errors.Add($"{owner} still uses the built-in Standard shader on {renderers[i].name}.");
                    }
                }
            }
        }
    }
}
