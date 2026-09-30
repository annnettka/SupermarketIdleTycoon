using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Checkout;
using SupermarketTycoon.Core;
using SupermarketTycoon.Customers;
using SupermarketTycoon.Employees;
using SupermarketTycoon.Expansion;
using SupermarketTycoon.Objectives;
using SupermarketTycoon.Products;
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
        private const string ObjectiveConfigPath = DataRoot + "/ObjectiveConfig.asset";
        private const string EmployeeDefinitionPath = DataRoot + "/CashierEmployee.asset";
        private const string StoreExpansionDefinitionPath = DataRoot + "/StoreExpansion.asset";
        private const string NormalCustomerProfilePath = DataRoot + "/CustomerNormal.asset";
        private const string ImpatientCustomerProfilePath = DataRoot + "/CustomerImpatient.asset";
        private const string VipCustomerProfilePath = DataRoot + "/CustomerVip.asset";
        private const string ShelfDefinitionPath = DataRoot + "/ShelfBuilding.asset";
        private const string CheckoutDefinitionPath = DataRoot + "/CheckoutBuilding.asset";
        private const string ShelfExpansionDefinitionPath = DataRoot + "/ShelfExpansion.asset";
        private const string StoreExpansionShelfDefinitionPath = DataRoot + "/StoreExpansionShelf.asset";
        private const string CheckoutExpansionDefinitionPath = DataRoot + "/CheckoutExpansion.asset";
        private const string PremiumShelfDefinitionPath = DataRoot + "/PremiumShelf.asset";

        private const string ShelfPrefabPath = PrefabRoot + "/Buildings/ShelfBuilding.prefab";
        private const string CheckoutPrefabPath = PrefabRoot + "/Buildings/CheckoutBuilding.prefab";
        private const string CustomerPrefabPath = PrefabRoot + "/Characters/Customer.prefab";
        private const string CashierPrefabPath = PrefabRoot + "/Characters/Cashier.prefab";
        private const string FloatingIncomePrefabPath = PrefabRoot + "/UI/FloatingIncome.prefab";
        private const string PurchaseFxPrefabPath = PrefabRoot + "/VFX/BuildingPurchaseFX.prefab";

        private const string SourceShelfPath = "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Shelves/Shelf_Flat.prefab";
        private const string SourceMiniShelfPath = "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Shelves/Stand_Mini.prefab";
        private const string SourceCheckoutPath = "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Shop/Cachier.prefab";
        private const string SourceCharacterPath = "Assets/Hodaart/HodaartLowPolyCharacterCollection3/Prefabs/Character 01.prefab";
        private const string SourceFloorPath = "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Building/Floor_Squared_Gray.prefab";
        private const string SourceWallPath = "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Building/Wall_Green_HalfDetail.prefab";
        private const string SourceDoorWallPath = "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Building/Wall_Flat_Green_HalfDetail_Doored.prefab";
        private const string SourceDustFxPath = "Assets/SimpleFX/Prefabs/FX_Dust_Prefab_01.prefab";

        private static readonly string[] CustomerVisualSourcePaths =
        {
            "Assets/Hodaart/HodaartLowPolyCharacterCollection3/Prefabs/Character 01.prefab",
            "Assets/Hodaart/HodaartLowPolyCharacterCollection3/Prefabs/Character 02.prefab",
            "Assets/Hodaart/HodaartLowPolyCharacterCollection3/Prefabs/Character 03.prefab",
            "Assets/Hodaart/HodaartLowPolyCharacterCollection3/Prefabs/Character 04.prefab",
            "Assets/Hodaart/HodaartLowPolyCharacterCollection3/Prefabs/Character 05.prefab"
        };

        private static readonly string[] RequiredFolders =
        {
            Root,
            DataRoot,
            MaterialRoot,
            PrefabRoot,
            PrefabRoot + "/Buildings",
            PrefabRoot + "/Characters",
            PrefabRoot + "/Products",
            PrefabRoot + "/Environment",
            PrefabRoot + "/UI",
            PrefabRoot + "/VFX",
            DataRoot + "/Products",
            Root + "/Audio",
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
                EnsureAudioAssets();
                var gameConfig = LoadOrCreateConfig<GameConfig>(GameConfigPath);
                var customerConfig = LoadOrCreateConfig<CustomerConfig>(CustomerConfigPath);
                var progressionConfig = LoadOrCreateConfig<SupermarketTycoon.Progression.ProgressionConfig>(ProgressionConfigPath);
                ConfigureGameplayConfigs(gameConfig, customerConfig, progressionConfig);
                var objectiveConfig = CreateObjectiveConfig();
                var employeeDefinition = CreateEmployeeDefinition();
                var expansionDefinition = CreateStoreExpansionDefinition();
                var customerProfiles = CreateCustomerProfiles();

                CreateProjectMaterials();
                MaterialRepairTool.EnsureFixedMaterials();
                var products = CreateProductAssets();
                CreateEnvironmentPrefabs(products);
                CreatePurchaseFxPrefab();
                CreateShelfPrefab(products, ShelfPrefabPath, false);
                CreateShelfPrefab(products, PremiumShelfPrefabPath, true);
                CreateCheckoutPrefab();
                CreateCustomerPrefab(products);
                CreateCashierPrefab();
                CreateFloatingIncomePrefab();

                var shelf = CreateBuildingDefinition(
                    ShelfDefinitionPath,
                    "shelf.basic",
                    "Shelf",
                    BuildingType.Shelf,
                    ShelfPrefabPath,
                    50,
                    1,
                    1,
                    new[]
                    {
                        new BuildingLevelDefinition(1, 0, 1, 1, 1f, 2.25f, 1f),
                        new BuildingLevelDefinition(2, 100, 1, 2, 1.25f, 1.9f, 1.04f),
                        new BuildingLevelDefinition(3, 250, 4, 3, 1.5f, 1.6f, 1.08f)
                    });
                var checkout = CreateBuildingDefinition(
                    CheckoutDefinitionPath,
                    "checkout.basic",
                    "Checkout",
                    BuildingType.Checkout,
                    CheckoutPrefabPath,
                    100,
                    1,
                    3,
                    new[]
                    {
                        new BuildingLevelDefinition(1, 0, 1, 3, 1f, 2.5f, 1f),
                        new BuildingLevelDefinition(2, 150, 1, 4, 1f, 1.8f, 1.04f),
                        new BuildingLevelDefinition(3, 350, 3, 5, 1f, 1.2f, 1.08f)
                    });
                var shelfExpansion = CreateBuildingDefinition(
                    ShelfExpansionDefinitionPath,
                    "shelf.expansion",
                    "Shelf",
                    BuildingType.Shelf,
                    ShelfPrefabPath,
                    150,
                    2,
                    1,
                    new[]
                    {
                        new BuildingLevelDefinition(1, 0, 2, 1, 1f, 2.25f, 1f),
                        new BuildingLevelDefinition(2, 175, 2, 2, 1.25f, 1.9f, 1.04f),
                        new BuildingLevelDefinition(3, 275, 4, 3, 1.5f, 1.6f, 1.08f)
                    });
                var storeExpansionShelf = CreateBuildingDefinition(
                    StoreExpansionShelfDefinitionPath,
                    "shelf.store-expansion",
                    "Expansion Shelf",
                    BuildingType.Shelf,
                    ShelfPrefabPath,
                    225,
                    3,
                    2,
                    new[]
                    {
                        new BuildingLevelDefinition(1, 0, 3, 2, 1.2f, 2.1f, 1.02f),
                        new BuildingLevelDefinition(2, 300, 4, 3, 1.55f, 1.7f, 1.08f),
                        new BuildingLevelDefinition(3, 500, 5, 4, 1.9f, 1.4f, 1.13f)
                    });
                var checkoutExpansion = CreateBuildingDefinition(
                    CheckoutExpansionDefinitionPath,
                    "checkout.expansion",
                    "Checkout",
                    BuildingType.Checkout,
                    CheckoutPrefabPath,
                    225,
                    3,
                    3,
                    new[]
                    {
                        new BuildingLevelDefinition(1, 0, 3, 3, 1f, 2.5f, 1f),
                        new BuildingLevelDefinition(2, 225, 3, 4, 1f, 1.8f, 1.04f),
                        new BuildingLevelDefinition(3, 400, 4, 5, 1f, 1.2f, 1.08f)
                    });
                var premiumShelf = CreateBuildingDefinition(
                    PremiumShelfDefinitionPath,
                    "shelf.premium",
                    "Premium Shelf",
                    BuildingType.Shelf,
                    PremiumShelfPrefabPath,
                    350,
                    4,
                    2,
                    new[]
                    {
                        new BuildingLevelDefinition(1, 0, 4, 2, 2f, 2f, 1.06f),
                        new BuildingLevelDefinition(2, 450, 5, 3, 2.5f, 1.6f, 1.12f)
                    });

                BuildBootstrapScene(gameConfig);
                BuildMainMenuScene();
                BuildGameScene(
                    gameConfig,
                    customerConfig,
                    progressionConfig,
                    shelf,
                    checkout,
                    shelfExpansion,
                    storeExpansionShelf,
                    checkoutExpansion,
                    premiumShelf,
                    customerProfiles,
                    objectiveConfig,
                    employeeDefinition,
                    expansionDefinition);
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

        private static void ConfigureGameplayConfigs(
            GameConfig gameConfig,
            CustomerConfig customerConfig,
            SupermarketTycoon.Progression.ProgressionConfig progressionConfig)
        {
            var game = new SerializedObject(gameConfig);
            game.FindProperty("startingMoney").intValue = 150;
            game.FindProperty("customerPayment").intValue = 20;
            game.FindProperty("customerXpReward").intValue = 10;
            game.FindProperty("spawnInterval").floatValue = 2.75f;
            game.FindProperty("maximumActiveCustomers").intValue = 10;
            game.ApplyModifiedPropertiesWithoutUndo();

            var customer = new SerializedObject(customerConfig);
            SetIntArray(customer.FindProperty("maximumActiveByLevel"), 3, 4, 6, 8, 10);
            customer.ApplyModifiedPropertiesWithoutUndo();

            var progression = new SerializedObject(progressionConfig);
            SetIntArray(progression.FindProperty("xpRequiredPerLevel"), 100, 200, 350, 450);
            var unlocks = progression.FindProperty("unlockSummaries");
            var values = new[]
            {
                "Shelf and Checkout",
                "Second Shelf and Cashier",
                "Store Expansion, Expansion Shelf, and Second Checkout",
                "VIP Customers and Premium Shelf",
                "Maximum Customer Flow"
            };
            unlocks.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                unlocks.GetArrayElementAtIndex(i).stringValue = values[i];
            }

            progression.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameConfig);
            EditorUtility.SetDirty(customerConfig);
            EditorUtility.SetDirty(progressionConfig);
        }

        private static ObjectiveConfig CreateObjectiveConfig()
        {
            var config = LoadOrCreateConfig<ObjectiveConfig>(ObjectiveConfigPath);
            config.Configure(new[]
            {
                new ObjectiveDefinition(ObjectiveType.BuildBuilding, "Build a Shelf", 1, "shelf.basic", 0, 20),
                new ObjectiveDefinition(ObjectiveType.BuildBuilding, "Build a Checkout", 1, "checkout.basic", 30, 20),
                new ObjectiveDefinition(ObjectiveType.ServeCustomers, "Serve 5 Customers", 5, null, 100, 30),
                new ObjectiveDefinition(ObjectiveType.UpgradeBuilding, "Upgrade a Shelf", 1, "shelf.basic", 0, 30),
                new ObjectiveDefinition(ObjectiveType.ReachLevel, "Reach Level 2", 2, null, 75, 0),
                new ObjectiveDefinition(ObjectiveType.BuyEmployee, "Hire a Cashier", 1, "employee.cashier", 75, 30),
                new ObjectiveDefinition(ObjectiveType.ServeCustomers, "Serve 15 Customers", 15, null, 100, 40),
                new ObjectiveDefinition(ObjectiveType.ReachLevel, "Reach Level 3", 3, null, 100, 0),
                new ObjectiveDefinition(ObjectiveType.BuyExpansion, "Unlock Store Expansion", 1, "expansion.main", 150, 50),
                new ObjectiveDefinition(ObjectiveType.BuildBuilding, "Build the Second Checkout", 1, "checkout.expansion", 200, 75),
                new ObjectiveDefinition(ObjectiveType.ReachRating, "Reach Store Rating 4.0", 40, null, 200, 100),
                new ObjectiveDefinition(ObjectiveType.ReachLevel, "Reach Level 5", 5, null, 500, 0)
            });
            EditorUtility.SetDirty(config);
            return config;
        }

        private static EmployeeDefinition CreateEmployeeDefinition()
        {
            var definition = LoadOrCreateConfig<EmployeeDefinition>(EmployeeDefinitionPath);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = "employee.cashier";
            serialized.FindProperty("displayName").stringValue = "Cashier";
            var levels = serialized.FindProperty("levels");
            var values = new[]
            {
                new EmployeeLevelDefinition(1, 300, 2, 0.2f),
                new EmployeeLevelDefinition(2, 450, 3, 0.35f),
                new EmployeeLevelDefinition(3, 700, 4, 0.5f)
            };
            levels.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                var element = levels.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("level").intValue = values[i].Level;
                element.FindPropertyRelative("cost").intValue = values[i].Cost;
                element.FindPropertyRelative("requiredPlayerLevel").intValue = values[i].RequiredPlayerLevel;
                element.FindPropertyRelative("checkoutSpeedBonus").floatValue = values[i].CheckoutSpeedBonus;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static StoreExpansionDefinition CreateStoreExpansionDefinition()
        {
            var definition = LoadOrCreateConfig<StoreExpansionDefinition>(StoreExpansionDefinitionPath);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = "expansion.main";
            serialized.FindProperty("displayName").stringValue = "Store Expansion";
            serialized.FindProperty("cost").intValue = 500;
            serialized.FindProperty("requiredLevel").intValue = 3;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static CustomerProfileDefinition[] CreateCustomerProfiles()
        {
            return new[]
            {
                CreateCustomerProfile(NormalCustomerProfilePath, "customer.normal", "Normal", 1f, 1f, 1f, 15f, 0.65f, 1, Color.white),
                CreateCustomerProfile(ImpatientCustomerProfilePath, "customer.impatient", "Impatient", 1.18f, 0.8f, 0.85f, 8f, 0.35f, 1, new Color(1f, 0.72f, 0.6f)),
                CreateCustomerProfile(VipCustomerProfilePath, "customer.vip", "VIP", 0.9f, 1.1f, 2f, 20f, 0.25f, 4, new Color(1f, 0.88f, 0.35f))
            };
        }

        private static CustomerProfileDefinition CreateCustomerProfile(
            string path,
            string id,
            string displayName,
            float movement,
            float shopping,
            float payment,
            float patience,
            float weight,
            int requiredLevel,
            Color color)
        {
            var profile = LoadOrCreateConfig<CustomerProfileDefinition>(path);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("movementSpeedMultiplier").floatValue = movement;
            serialized.FindProperty("shoppingTimeMultiplier").floatValue = shopping;
            serialized.FindProperty("paymentMultiplier").floatValue = payment;
            serialized.FindProperty("queuePatience").floatValue = patience;
            serialized.FindProperty("spawnWeight").floatValue = weight;
            serialized.FindProperty("requiredLevel").intValue = requiredLevel;
            serialized.FindProperty("presentationColor").colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void SetIntArray(SerializedProperty property, params int[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).intValue = values[i];
            }
        }

        private static BuildingDefinition CreateBuildingDefinition(
            string path,
            string id,
            string displayName,
            BuildingType type,
            string prefabPath,
            int cost,
            int requiredLevel,
            int capacity,
            BuildingLevelDefinition[] levels)
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
            var levelsProperty = serialized.FindProperty("upgradeLevels");
            levelsProperty.arraySize = levels != null ? levels.Length : 0;
            for (var i = 0; i < levelsProperty.arraySize; i++)
            {
                var element = levelsProperty.GetArrayElementAtIndex(i);
                var level = levels[i];
                element.FindPropertyRelative("level").intValue = level.Level;
                element.FindPropertyRelative("upgradeCost").intValue = level.UpgradeCost;
                element.FindPropertyRelative("requiredPlayerLevel").intValue = level.RequiredPlayerLevel;
                element.FindPropertyRelative("capacity").intValue = level.Capacity;
                element.FindPropertyRelative("incomeMultiplier").floatValue = level.IncomeMultiplier;
                element.FindPropertyRelative("interactionDuration").floatValue = level.InteractionDuration;
                element.FindPropertyRelative("visualScale").floatValue = level.VisualScale;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void CreateShelfPrefab(
            ProductDefinition[] products,
            string prefabPath,
            bool premium)
        {
            var root = new GameObject(premium ? "PremiumShelfBuilding" : "ShelfBuilding");
            try
            {
                var source = AddNestedVisual(root.transform, SourceShelfPath, "Shelf Visual");
                if (source == null)
                {
                    CreateFallbackPrimitive(root.transform, PrimitiveType.Cube, "Shelf Visual", new Vector3(2.8f, 1.8f, 0.8f), StoreGreen);
                }

                if (premium)
                {
                    var accent = CreateFallbackPrimitive(
                        root.transform,
                        PrimitiveType.Cube,
                        "Premium Display Base",
                        new Vector3(3.45f, 0.12f, 1.08f),
                        AccentYellow);
                    accent.transform.localPosition = new Vector3(0f, 0.07f, 0f);
                    UnityEngine.Object.DestroyImmediate(accent.GetComponent<Collider>());
                }

                var stock = CreateShelfStock(root.transform, products, premium);

                var point = new GameObject("InteractionPoint").transform;
                point.SetParent(root.transform, false);
                point.localPosition = new Vector3(0f, 0f, -1.35f);
                var station = root.AddComponent<ShelfStation>();
                station.Configure(point, premium ? 2f : 2.25f, stock);
                root.AddComponent<BuildingUpgradeFeedback>();
                AddPurchaseFx(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
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

                var points = new Transform[5];
                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = new GameObject($"QueuePoint{i}").transform;
                    points[i].SetParent(root.transform, false);
                    points[i].localPosition = new Vector3(0f, 0f, -1.35f - i * 1.15f);
                }

                var station = root.AddComponent<CheckoutStation>();
                station.Configure(points);
                root.AddComponent<BuildingUpgradeFeedback>();
                AddPurchaseFx(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, CheckoutPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateCustomerPrefab(ProductDefinition[] products)
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

                var visualSelector = root.AddComponent<CustomerVisualSelector>();
                var variants = new CustomerVisualVariant[CustomerVisualSourcePaths.Length];
                for (var i = 0; i < CustomerVisualSourcePaths.Length; i++)
                {
                    var visual = AddNestedVisual(
                        root.transform,
                        CustomerVisualSourcePaths[i],
                        $"Customer Visual {i + 1}");
                    if (visual != null)
                    {
                        visual.transform.localScale = Vector3.one * 0.82f;
                    }
                    else
                    {
                        visual = CreateFallbackPrimitive(
                            root.transform,
                            PrimitiveType.Capsule,
                            $"Customer Visual {i + 1}",
                            new Vector3(0.62f + i * 0.04f, 0.82f + i * 0.03f, 0.62f),
                            Color.HSVToRGB(i / (float)CustomerVisualSourcePaths.Length, 0.55f, 0.9f));
                        visual.transform.localPosition = Vector3.up * 0.9f;
                    }

                    variants[i] = new CustomerVisualVariant($"customer.visual.{i + 1:00}", visual);
                }

                visualSelector.Configure(variants);
                var carryView = CreateProductCarryView(root.transform, products);
                var customer = root.AddComponent<CustomerAgent>();
                customer.Configure(navigation, visualSelector, carryView);

                PrefabUtility.SaveAsPrefabAsset(root, CustomerPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateCashierPrefab()
        {
            var root = new GameObject("CashierEmployee");
            try
            {
                var source = AddNestedVisual(root.transform, SourceCharacterPath, "Cashier Visual");
                if (source != null)
                {
                    source.transform.localScale = Vector3.one * 0.82f;
                    source.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }
                else
                {
                    var fallback = CreateFallbackPrimitive(root.transform, PrimitiveType.Capsule, "Cashier Visual", new Vector3(0.7f, 0.9f, 0.7f), AccentYellow);
                    fallback.transform.localPosition = Vector3.up * 0.9f;
                }

                PrefabUtility.SaveAsPrefabAsset(root, CashierPrefabPath);
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
            MaterialRepairTool.ApplyKnownReplacements(instance);
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
