using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Checkout;
using SupermarketTycoon.Core;
using SupermarketTycoon.Customers;
using SupermarketTycoon.UI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SupermarketTycoon.Editor
{
    public static partial class SupermarketTycoonDemoBuilder
    {
        private const string MenuPath = "Tools/Supermarket Tycoon/Build / Repair Playable Demo";
        private const string AutoBuildSessionKey = "SupermarketTycoon.DemoBuilder.AutoBuild.1";

        private const string Root = "Assets/_Project";
        private const string DataRoot = Root + "/Data/ScriptableObjects";
        private const string PrefabRoot = Root + "/Art/Prefabs";
        private const string MaterialRoot = Root + "/Art/Materials";
        private const string SceneRoot = Root + "/Scenes";

        private const string BootstrapScenePath = SceneRoot + "/Bootstrap.unity";
        private const string MainMenuScenePath = SceneRoot + "/MainMenu.unity";
        private const string GameScenePath = SceneRoot + "/Game.unity";

        private const string GameConfigPath = DataRoot + "/GameConfig.asset";
        private const string CustomerConfigPath = DataRoot + "/CustomerConfig.asset";
        private const string ProgressionConfigPath = DataRoot + "/ProgressionConfig.asset";
        private const string ShelfDefinitionPath = DataRoot + "/ShelfBuilding.asset";
        private const string CheckoutDefinitionPath = DataRoot + "/CheckoutBuilding.asset";
        private const string ShelfExpansionDefinitionPath = DataRoot + "/ShelfExpansion.asset";
        private const string CheckoutExpansionDefinitionPath = DataRoot + "/CheckoutExpansion.asset";

        private const string ShelfPrefabPath = PrefabRoot + "/Buildings/ShelfBuilding.prefab";
        private const string CheckoutPrefabPath = PrefabRoot + "/Buildings/CheckoutBuilding.prefab";
        private const string CustomerPrefabPath = PrefabRoot + "/Characters/Customer.prefab";
        private const string FloatingIncomePrefabPath = PrefabRoot + "/UI/FloatingIncome.prefab";
        private const string PurchaseFxPrefabPath = PrefabRoot + "/VFX/BuildingPurchaseFX.prefab";

        private const string SourceShelfPath = "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Shelves/Shelf_Flat.prefab";
        private const string SourceCheckoutPath = "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Shop/Cachier.prefab";
        private const string SourceCharacterPath = "Assets/Hodaart/HodaartLowPolyCharacterCollection3/Prefabs/Character 01.prefab";
        private const string SourceFloorPath = "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Building/Floor_Squared_Gray.prefab";
        private const string SourceWallPath = "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Building/Wall_Green_HalfDetail.prefab";
        private const string SourceDoorWallPath = "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Building/Wall_Flat_Green_HalfDetail_Doored.prefab";
        private const string SourceDustFxPath = "Assets/SimpleFX/Prefabs/FX_Dust_Prefab_01.prefab";

        private static readonly string[] RequiredFolders =
        {
            Root,
            DataRoot,
            MaterialRoot,
            PrefabRoot,
            PrefabRoot + "/Buildings",
            PrefabRoot + "/Characters",
            PrefabRoot + "/UI",
            PrefabRoot + "/VFX",
            SceneRoot,
            Root + "/Tests/EditMode"
        };

        [InitializeOnLoadMethod]
        private static void ScheduleOneTimeBuild()
        {
            if (Application.isBatchMode || SessionState.GetBool(AutoBuildSessionKey, false))
            {
                return;
            }

            if (File.Exists(ToAbsolutePath(BootstrapScenePath)) ||
                File.Exists(ToAbsolutePath(MainMenuScenePath)) ||
                File.Exists(ToAbsolutePath(GameScenePath)))
            {
                return;
            }

            SessionState.SetBool(AutoBuildSessionKey, true);
            EditorApplication.delayCall += () => BuildPlayableDemo(true);
        }

        [InitializeOnLoadMethod]
        private static void SchedulePlayModeBootstrapConfiguration()
        {
            if (!Application.isBatchMode)
            {
                EditorApplication.delayCall += ConfigurePlayModeStartScene;
            }
        }

        [MenuItem(MenuPath)]
        public static void BuildPlayableDemoFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Playable demo build cancelled because modified scenes were not saved.");
                return;
            }

            BuildPlayableDemo(false);
        }

        public static void BuildPlayableDemoBatch()
        {
            BuildPlayableDemo(true);
        }

        private static void BuildPlayableDemo(bool automatic)
        {
            try
            {
                EnsureFolders();
                var gameConfig = LoadOrCreateConfig<GameConfig>(GameConfigPath);
                var customerConfig = LoadOrCreateConfig<CustomerConfig>(CustomerConfigPath);
                var progressionConfig = LoadOrCreateConfig<SupermarketTycoon.Progression.ProgressionConfig>(ProgressionConfigPath);

                CreateProjectMaterials();
                CreatePurchaseFxPrefab();
                CreateShelfPrefab();
                CreateCheckoutPrefab();
                CreateCustomerPrefab();
                CreateFloatingIncomePrefab();

                var shelf = CreateBuildingDefinition(
                    ShelfDefinitionPath,
                    "shelf.basic",
                    "Shelf",
                    BuildingType.Shelf,
                    ShelfPrefabPath,
                    50,
                    1,
                    1);
                var checkout = CreateBuildingDefinition(
                    CheckoutDefinitionPath,
                    "checkout.basic",
                    "Checkout",
                    BuildingType.Checkout,
                    CheckoutPrefabPath,
                    100,
                    1,
                    3);
                var shelfExpansion = CreateBuildingDefinition(
                    ShelfExpansionDefinitionPath,
                    "shelf.expansion",
                    "Shelf",
                    BuildingType.Shelf,
                    ShelfPrefabPath,
                    75,
                    2,
                    1);
                var checkoutExpansion = CreateBuildingDefinition(
                    CheckoutExpansionDefinitionPath,
                    "checkout.expansion",
                    "Checkout",
                    BuildingType.Checkout,
                    CheckoutPrefabPath,
                    150,
                    3,
                    3);

                BuildBootstrapScene(gameConfig);
                BuildMainMenuScene();
                BuildGameScene(
                    gameConfig,
                    customerConfig,
                    progressionConfig,
                    shelf,
                    checkout,
                    shelfExpansion,
                    checkoutExpansion);
                ConfigureBuildSettings();
                ConfigurePlayModeStartScene();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    "Supermarket Tycoon playable demo build complete. " +
                    $"Mode: {(automatic ? "automatic first build" : "manual repair")}. " +
                    "Third-party source assets were referenced without modification.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    throw;
                }
            }
        }

        private static void EnsureFolders()
        {
            foreach (var folder in RequiredFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                var slash = folder.LastIndexOf('/');
                var parent = folder.Substring(0, slash);
                var name = folder.Substring(slash + 1);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static T LoadOrCreateConfig<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static BuildingDefinition CreateBuildingDefinition(
            string path,
            string id,
            string displayName,
            BuildingType type,
            string prefabPath,
            int cost,
            int requiredLevel,
            int capacity)
        {
            var definition = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BuildingDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("type").enumValueIndex = (int)type;
            serialized.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            serialized.FindProperty("cost").intValue = cost;
            serialized.FindProperty("requiredLevel").intValue = requiredLevel;
            serialized.FindProperty("capacity").intValue = capacity;
            serialized.FindProperty("incomeMultiplier").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void CreateShelfPrefab()
        {
            var root = new GameObject("ShelfBuilding");
            try
            {
                var source = AddNestedVisual(root.transform, SourceShelfPath, "Shelf Visual");
                if (source == null)
                {
                    CreateFallbackPrimitive(root.transform, PrimitiveType.Cube, "Shelf Visual", new Vector3(2.8f, 1.8f, 0.8f), StoreGreen);
                }

                var point = new GameObject("InteractionPoint").transform;
                point.SetParent(root.transform, false);
                point.localPosition = new Vector3(0f, 0f, -1.35f);
                var station = root.AddComponent<ShelfStation>();
                station.Configure(point, 2.25f);
                AddPurchaseFx(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, ShelfPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateCheckoutPrefab()
        {
            var root = new GameObject("CheckoutBuilding");
            try
            {
                var source = AddNestedVisual(root.transform, SourceCheckoutPath, "Checkout Visual");
                if (source == null)
                {
                    CreateFallbackPrimitive(root.transform, PrimitiveType.Cube, "Checkout Visual", new Vector3(2.5f, 1.1f, 1f), CheckoutBlue);
                }

                var points = new Transform[3];
                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = new GameObject($"QueuePoint{i}").transform;
                    points[i].SetParent(root.transform, false);
                    points[i].localPosition = new Vector3(0f, 0f, -1.35f - i * 1.15f);
                }

                var station = root.AddComponent<CheckoutStation>();
                station.Configure(points);
                AddPurchaseFx(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, CheckoutPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateCustomerPrefab()
        {
            var root = new GameObject("Customer");
            try
            {
                var navigation = root.AddComponent<NavMeshAgent>();
                navigation.radius = 0.32f;
                navigation.height = 1.9f;
                navigation.speed = 3.5f;
                navigation.acceleration = 14f;
                navigation.angularSpeed = 720f;
                navigation.stoppingDistance = 0.15f;

                var customer = root.AddComponent<CustomerAgent>();
                customer.Configure(navigation);
                var source = AddNestedVisual(root.transform, SourceCharacterPath, "Character Visual");
                if (source != null)
                {
                    source.transform.localScale = Vector3.one * 0.82f;
                }
                else
                {
                    var fallback = CreateFallbackPrimitive(root.transform, PrimitiveType.Capsule, "Character Visual", new Vector3(0.7f, 0.9f, 0.7f), AccentYellow);
                    fallback.transform.localPosition = Vector3.up * 0.9f;
                }

                PrefabUtility.SaveAsPrefabAsset(root, CustomerPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreatePurchaseFxPrefab()
        {
            var root = new GameObject("BuildingPurchaseFX");
            try
            {
                var source = AddNestedVisual(root.transform, SourceDustFxPath, "Dust FX");
                if (source == null)
                {
                    var particle = root.AddComponent<ParticleSystem>();
                    var main = particle.main;
                    main.duration = 0.5f;
                    main.startLifetime = 0.45f;
                    main.startSpeed = 2f;
                    main.startColor = AccentYellow;
                    main.loop = false;
                    var emission = particle.emission;
                    emission.rateOverTime = 0f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
                }

                PrefabUtility.SaveAsPrefabAsset(root, PurchaseFxPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AddPurchaseFx(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PurchaseFxPrefabPath);
            if (prefab == null)
            {
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Purchase FX";
            instance.transform.SetParent(parent, false);
        }

        private static GameObject AddNestedVisual(Transform parent, string path, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"Optional source visual not found at '{path}'. A native fallback will be used.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            return instance;
        }

        private static GameObject CreateFallbackPrimitive(
            Transform parent,
            PrimitiveType type,
            string name,
            Vector3 scale,
            Color color)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localScale = scale;
            primitive.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(SanitizeFileName(name), color);
            return primitive;
        }

        private static void CreateProjectMaterials()
        {
            GetOrCreateMaterial("StoreGreen", StoreGreen);
            GetOrCreateMaterial("CheckoutBlue", CheckoutBlue);
            GetOrCreateMaterial("AccentYellow", AccentYellow);
            GetOrCreateMaterial("Floor", FloorColor);
            GetOrCreateMaterial("Wall", WallColor);
            GetOrCreateMaterial("BuildSpot", BuildSpotColor);
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            var path = $"{MaterialRoot}/{SanitizeFileName(name)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string SanitizeFileName(string value)
        {
            return value.Replace(" ", string.Empty).Replace("/", string.Empty);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var relative = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, relative);
        }

        private static void ConfigureBuildSettings()
        {
            var required = new[] { BootstrapScenePath, MainMenuScenePath, GameScenePath };
            var existing = EditorBuildSettings.scenes;
            var updated = new List<EditorBuildSettingsScene>();

            for (var i = 0; i < required.Length; i++)
            {
                updated.Add(new EditorBuildSettingsScene(required[i], true));
            }

            foreach (var scene in existing)
            {
                if (!required.Contains(scene.path, StringComparer.OrdinalIgnoreCase))
                {
                    updated.Add(scene);
                }
            }

            EditorBuildSettings.scenes = updated.ToArray();
        }

        private static void ConfigurePlayModeStartScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (bootstrapScene != null && EditorSceneManager.playModeStartScene != bootstrapScene)
            {
                EditorSceneManager.playModeStartScene = bootstrapScene;
            }
        }

        private static readonly Color StoreGreen = new Color(0.16f, 0.62f, 0.42f);
        private static readonly Color CheckoutBlue = new Color(0.12f, 0.46f, 0.72f);
        private static readonly Color AccentYellow = new Color(1f, 0.72f, 0.14f);
        private static readonly Color FloorColor = new Color(0.78f, 0.84f, 0.82f);
        private static readonly Color WallColor = new Color(0.92f, 0.96f, 0.93f);
        private static readonly Color BuildSpotColor = new Color(0.12f, 0.68f, 0.55f, 0.65f);
    }
}
