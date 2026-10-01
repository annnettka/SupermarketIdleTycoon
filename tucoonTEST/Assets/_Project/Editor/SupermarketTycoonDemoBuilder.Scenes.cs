using System;
using System.IO;
using SupermarketTycoon.Bootstrap;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Core;
using SupermarketTycoon.Customers;
using SupermarketTycoon.Employees;
using SupermarketTycoon.Expansion;
using SupermarketTycoon.Menu;
using SupermarketTycoon.Objectives;
using SupermarketTycoon.Progression;
using SupermarketTycoon.SceneFlow;
using SupermarketTycoon.UI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SupermarketTycoon.Editor
{
    /// <summary>
    /// Rebuilds the three owned scenes from scratch so repeated runs replace content instead of duplicating it.
    /// Перестраивает три собственные сцены с нуля, чтобы повторный запуск заменял содержимое, а не дублировал его.
    /// </summary>
    public static partial class SupermarketTycoonDemoBuilder
    {
        private static void BuildBootstrapScene(GameConfig gameConfig)
        {
            BuildOwnedScene(BootstrapScenePath, () =>
            {
                var root = new GameObject("Application");
                var bootstrapper = root.AddComponent<AppBootstrapper>();

                var mixer = EnsureAudioMixer();
                var audioRoot = new GameObject("Audio");
                audioRoot.transform.SetParent(root.transform, false);

                var musicSource = new GameObject("Music A").AddComponent<AudioSource>();
                musicSource.transform.SetParent(audioRoot.transform, false);
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.spatialBlend = 0f;
                musicSource.outputAudioMixerGroup = FindMixerGroup(mixer, "Music");

                var secondaryMusicSource = new GameObject("Music B").AddComponent<AudioSource>();
                secondaryMusicSource.transform.SetParent(audioRoot.transform, false);
                secondaryMusicSource.playOnAwake = false;
                secondaryMusicSource.loop = true;
                secondaryMusicSource.spatialBlend = 0f;
                secondaryMusicSource.outputAudioMixerGroup = FindMixerGroup(mixer, "Music");

                var sfxSource = new GameObject("SFX").AddComponent<AudioSource>();
                sfxSource.transform.SetParent(audioRoot.transform, false);
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
                sfxSource.outputAudioMixerGroup = FindMixerGroup(mixer, "SFX");

                var loading = CreateLoadingScreen(root.transform);
                SetReference(bootstrapper, "gameConfig", gameConfig);
                SetReference(bootstrapper, "loadingScreen", loading);
                SetReference(bootstrapper, "audioMixer", mixer);
                SetReference(bootstrapper, "musicSource", musicSource);
                SetReference(bootstrapper, "secondaryMusicSource", secondaryMusicSource);
                SetReference(bootstrapper, "sfxSource", sfxSource);
                SetReference(bootstrapper, "menuMusicClip", LoadAudio(MenuMusicPath));
                SetReference(bootstrapper, "gameplayMusicClip", LoadAudio(GameplayMusicPath));
                SetReference(bootstrapper, "uiClickClip", LoadAudio("Assets/Cartoon Game Sound 2.0/s_ef_cm_dm_umbrella_open.wav"));
                SetReference(bootstrapper, "buildClip", LoadAudio("Assets/Cartoon Game Sound 2.0/s_ef_ce_barrier.wav"));
                SetReference(bootstrapper, "incomeClip", LoadAudio("Assets/Cartoon Game Sound 2.0/s_ef_ce_yororo_e.wav"));
                SetReference(bootstrapper, "levelUpClip", LoadAudio("Assets/Cartoon Game Sound 2.0/s_ef_ke_at_huripen.wav"));
            });
        }

        private static void BuildMainMenuScene()
        {
            BuildOwnedScene(MainMenuScenePath, () =>
            {
                var camera = CreateIsometricCamera(new Vector3(11.8f, 9.2f, -14.5f), new Vector3(0f, 1.1f, 1.2f), false);
                camera.fieldOfView = 40f;
                camera.gameObject.AddComponent<MenuCameraDrift>().Configure(new Vector3(0.48f, 0.08f, 0.22f), 16f);
                CreateLighting();
                CreateMenuShowcaseEnvironment();

                AddScenePrefab(ShelfPrefabPath, new Vector3(-4.4f, 0f, 2.6f), Quaternion.Euler(0f, 8f, 0f), "Stocked Grocery Aisle");
                AddScenePrefab(PremiumShelfPrefabPath, new Vector3(0.2f, 0f, 4.15f), Quaternion.Euler(0f, -5f, 0f), "Premium Product Display");
                AddScenePrefab(CheckoutPrefabPath, new Vector3(4.4f, 0f, 0.1f), Quaternion.Euler(0f, 174f, 0f), "Showcase Checkout");
                AddScenePrefab(FreshDisplayPrefabPath, new Vector3(-5.3f, 0f, -0.4f), Quaternion.Euler(0f, 12f, 0f), "Fresh Food Display");
                AddScenePrefab(StorageCornerPrefabPath, new Vector3(5.1f, 0f, 4.45f), Quaternion.Euler(0f, 180f, 0f), "Staff Storage Detail");
                AddScenePrefab(SourceCharacterPath, new Vector3(-1.5f, 0f, 0.45f), Quaternion.Euler(0f, 145f, 0f), "Customer Display A", 0.82f);
                AddScenePrefab(CustomerVisualSourcePaths[2], new Vector3(1.5f, 0f, 2.1f), Quaternion.Euler(0f, 205f, 0f), "Customer Display B", 0.82f);
                AddScenePrefab(CustomerVisualSourcePaths[4], new Vector3(3.15f, 0f, -1.05f), Quaternion.Euler(0f, 150f, 0f), "Customer Display C", 0.82f);
                CreateMenuSparkles();

                EnsureEventSystem();
                var canvas = CreateScreenCanvas("Main Menu Canvas");
                CreateMainMenuUi(canvas.transform, out var mainMenuView, out var settingsView);

                var entry = new GameObject("MainMenu Entry Point").AddComponent<MainMenuEntryPoint>();
                entry.Configure(mainMenuView, settingsView);
            });
        }

        private static void BuildGameScene(
            GameConfig gameConfig,
            CustomerConfig customerConfig,
            ProgressionConfig progressionConfig,
            BuildingDefinition shelf,
            BuildingDefinition checkout,
            BuildingDefinition shelfExpansion,
            BuildingDefinition storeExpansionShelf,
            BuildingDefinition checkoutExpansion,
            BuildingDefinition premiumShelf,
            CustomerProfileDefinition[] customerProfiles,
            ObjectiveConfig objectiveConfig,
            EmployeeDefinition employeeDefinition,
            StoreExpansionDefinition expansionDefinition)
        {
            BuildOwnedScene(GameScenePath, () =>
            {
                var camera = CreateIsometricCamera(new Vector3(15.5f, 18.5f, -19.5f), new Vector3(0f, 0f, 0.4f), true);
                camera.orthographicSize = 11.1f;
                CreateLighting();
                CreateStoreEnvironment();
                EnsureEventSystem();

                var spots = new[]
                {
                    CreateWorldBuildSpot("spot.shelf.a", shelf, new Vector3(-4.8f, 0.12f, -1.5f), Quaternion.identity, camera),
                    CreateWorldBuildSpot("spot.checkout.a", checkout, new Vector3(3.6f, 0.12f, -5.75f), Quaternion.Euler(0f, 180f, 0f), camera),
                    CreateWorldBuildSpot("spot.shelf.b", shelfExpansion, new Vector3(-0.7f, 0.12f, 1.35f), Quaternion.identity, camera),
                    CreateWorldBuildSpot("spot.shelf.expansion", storeExpansionShelf, new Vector3(-0.7f, 0.12f, 5.65f), Quaternion.identity, camera, expansionDefinition.Id),
                    CreateWorldBuildSpot("spot.checkout.b", checkoutExpansion, new Vector3(6.3f, 0.12f, -5.75f), Quaternion.Euler(0f, 180f, 0f), camera, expansionDefinition.Id),
                    CreateWorldBuildSpot("spot.shelf.premium", premiumShelf, new Vector3(5.55f, 0.12f, 5.65f), Quaternion.identity, camera, expansionDefinition.Id)
                };

                var expansionSpot = CreateStoreExpansionSpot(expansionDefinition, camera);

                var cashierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CashierPrefabPath);
                GameObject cashierVisual = null;
                if (cashierPrefab != null)
                {
                    cashierVisual = (GameObject)PrefabUtility.InstantiatePrefab(cashierPrefab, SceneManager.GetActiveScene());
                    cashierVisual.name = "Cashier Employee";
                    cashierVisual.transform.SetPositionAndRotation(new Vector3(3.65f, 0f, -5.15f), Quaternion.Euler(0f, 180f, 0f));
                    cashierVisual.SetActive(false);
                }

                var navigationRoot = new GameObject("Navigation");
                var surface = navigationRoot.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.layerMask = ~0;

                var customerSystem = new GameObject("Customer System");
                var spawn = new GameObject("Customer Spawn").transform;
                spawn.SetParent(customerSystem.transform, false);
                spawn.position = new Vector3(-1.25f, 0f, -8.05f);
                spawn.rotation = Quaternion.identity;
                var exit = new GameObject("Customer Exit").transform;
                exit.SetParent(customerSystem.transform, false);
                exit.position = new Vector3(1.25f, 0f, -8.05f);
                exit.rotation = Quaternion.identity;

                var spawner = customerSystem.AddComponent<CustomerSpawner>();
                var customerPrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(CustomerPrefabPath);
                spawner.Configure(
                    customerPrefabObject != null ? customerPrefabObject.GetComponent<CustomerAgent>() : null,
                    spawn,
                    exit);

                var canvas = CreateScreenCanvas("Game Canvas");
                CreateGameUi(
                    canvas.transform,
                    out var hud,
                    out var buildingPanel,
                    out var employeePanel,
                    out var offlinePanel,
                    out var pauseMenu,
                    out var statsPanel,
                    out var settingsView);

                var entry = new GameObject("Game Entry Point").AddComponent<GameSceneEntryPoint>();
                entry.Configure(
                    gameConfig,
                    customerConfig,
                    progressionConfig,
                    customerProfiles,
                    objectiveConfig,
                    employeeDefinition,
                    spots,
                    new[] { expansionSpot },
                    cashierVisual,
                    spawner,
                    hud,
                    buildingPanel,
                    employeePanel,
                    offlinePanel,
                    pauseMenu,
                    statsPanel,
                    settingsView);

                surface.BuildNavMesh();
                EditorUtility.SetDirty(surface);
            });
        }

        private static void BuildOwnedScene(string path, Action build)
        {
            var previousActive = SceneManager.GetActiveScene();
            var scene = FindLoadedScene(path);
            var closeAfterBuild = false;

            if (!scene.IsValid())
            {
                scene = File.Exists(ToAbsolutePath(path))
                    ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                closeAfterBuild = true;
            }

            SceneManager.SetActiveScene(scene);
            var roots = scene.GetRootGameObjects();
            for (var i = roots.Length - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(roots[i]);
            }

            build();
            EditorSceneManager.SaveScene(scene, path);

            if (closeAfterBuild)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            if (previousActive.IsValid() && previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(previousActive);
            }
        }

        private static Scene FindLoadedScene(string path)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (string.Equals(scene.path, path, StringComparison.OrdinalIgnoreCase))
                {
                    return scene;
                }
            }

            return default;
        }

        private static Camera CreateIsometricCamera(Vector3 position, Vector3 target, bool orthographic)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = position;
            cameraObject.transform.LookAt(target);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = orthographic;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.82f, 0.9f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Sun Light");
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.68f, 0.72f, 0.7f);

            var fillObject = new GameObject("Store Fill Light");
            fillObject.transform.position = new Vector3(0f, 7f, 0.5f);
            fillObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 24f;
            fill.intensity = 0.55f;
            fill.color = new Color(0.78f, 0.9f, 1f);
            fill.shadows = LightShadows.None;
        }

        private static void CreateStoreEnvironment()
        {
            var environment = new GameObject("Store Environment").transform;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Walkable Floor";
            floor.transform.SetParent(environment, false);
            floor.transform.position = new Vector3(0f, -0.18f, 0f);
            floor.transform.localScale = new Vector3(20f, 0.35f, 18f);
            floor.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Floor", FloorColor);

            for (var x = -2; x <= 2; x++)
            {
                for (var z = -2; z <= 2; z++)
                {
                    AddScenePrefab(
                        SourceFloorPath,
                        new Vector3(x * 4f, 0.02f, z * 4f),
                        Quaternion.identity,
                        $"Floor Tile {x} {z}");
                }
            }

            CreateFloorZone(environment, "Fresh Food Zone", new Vector3(-6.4f, 0.015f, 1.35f), new Vector3(5f, 0.03f, 5.2f), new Color(0.78f, 0.93f, 0.67f));
            CreateFloorZone(environment, "Checkout Zone", new Vector3(5.1f, 0.016f, -5.2f), new Vector3(8.5f, 0.03f, 4.3f), new Color(0.62f, 0.82f, 0.93f));
            CreateFloorZone(environment, "Expansion Zone", new Vector3(0f, 0.017f, 6f), new Vector3(18.8f, 0.03f, 5.2f), new Color(0.86f, 0.82f, 0.67f));
            CreateFloorZone(environment, "Premium Goods Zone", new Vector3(6.15f, 0.019f, 5.75f), new Vector3(6.4f, 0.03f, 5f), new Color(0.95f, 0.76f, 0.34f));

            CreateWall(environment, new Vector3(0f, 1.3f, 8.85f), new Vector3(20f, 2.6f, 0.3f));
            CreateWall(environment, new Vector3(-9.85f, 1.3f, 0f), new Vector3(0.3f, 2.6f, 18f));
            CreateWall(environment, new Vector3(9.85f, 1.3f, 0f), new Vector3(0.3f, 2.6f, 18f));
            CreateWall(environment, new Vector3(-6.1f, 1.3f, -8.85f), new Vector3(7.6f, 2.6f, 0.3f));
            CreateWall(environment, new Vector3(6.1f, 1.3f, -8.85f), new Vector3(7.6f, 2.6f, 0.3f));

            AddScenePrefab(SourceWallPath, new Vector3(0f, 0f, 8.6f), Quaternion.identity, "Grocery Rear Wall");
            AddScenePrefab(SourceDoorWallPath, new Vector3(0f, 0f, -8.62f), Quaternion.Euler(0f, 180f, 0f), "Grocery Entrance");
            AddScenePrefab(EntranceDecorPrefabPath, new Vector3(0f, 0.02f, -7.65f), Quaternion.identity, "Entrance Welcome Area");
            AddScenePrefab(FreshDisplayPrefabPath, new Vector3(-7.2f, 0f, 0.15f), Quaternion.Euler(0f, 90f, 0f), "Fresh Food Island");
            AddScenePrefab(CheckoutImpulsePrefabPath, new Vector3(7.55f, 0f, -3.65f), Quaternion.Euler(0f, 180f, 0f), "Checkout Impulse Display");

            CreateZoneSign(environment, "FRESH MARKET", new Vector3(-6.25f, 2.25f, 8.65f), new Color(0.2f, 0.5f, 0.22f));
            CreateZoneSign(environment, "GROCERIES", new Vector3(0f, 2.25f, 8.65f), new Color(0.08f, 0.32f, 0.34f));
            CreateZoneSign(environment, "PREMIUM GOODS", new Vector3(6.25f, 2.25f, 8.65f), new Color(0.68f, 0.45f, 0.05f));

            var entranceSign = new GameObject("Entrance Sign");
            entranceSign.transform.SetParent(environment, false);
            entranceSign.transform.position = new Vector3(0f, 2.8f, -8.8f);
        }

        private static void CreateMenuShowcaseEnvironment()
        {
            var environment = new GameObject("Menu Supermarket Showcase").transform;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Showcase Floor";
            floor.transform.SetParent(environment, false);
            floor.transform.position = new Vector3(0f, -0.15f, 1f);
            floor.transform.localScale = new Vector3(17f, 0.3f, 13f);
            floor.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Floor", FloorColor);

            CreateWall(environment, new Vector3(0f, 1.45f, 6.85f), new Vector3(17f, 2.9f, 0.25f));
            CreateWall(environment, new Vector3(-8.35f, 1.45f, 1f), new Vector3(0.25f, 2.9f, 12f));
            CreateWall(environment, new Vector3(8.35f, 1.45f, 1f), new Vector3(0.25f, 2.9f, 12f));
            CreateFloorZone(environment, "Menu Fresh Zone", new Vector3(-5.3f, 0.02f, 0f), new Vector3(4.2f, 0.03f, 4.6f), new Color(0.77f, 0.92f, 0.67f));
            CreateFloorZone(environment, "Menu Checkout Zone", new Vector3(4.4f, 0.02f, 0f), new Vector3(4.8f, 0.03f, 4.6f), new Color(0.62f, 0.82f, 0.93f));
        }

        private static void CreateMenuSparkles()
        {
            var particlesObject = new GameObject("Menu Ambient Sparkles");
            particlesObject.transform.position = new Vector3(0f, 2.2f, 0.8f);
            var particles = particlesObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.78f, 0.2f, 0.3f), new Color(1f, 1f, 1f, 0.55f));
            main.maxParticles = 40;
            var emission = particles.emission;
            emission.rateOverTime = 4f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(9f, 3.5f, 5f);
        }

        private static void CreateFloorZone(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.name = name;
            zone.transform.SetParent(parent, false);
            zone.transform.position = position;
            zone.transform.localScale = scale;
            zone.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(name, color);
            UnityEngine.Object.DestroyImmediate(zone.GetComponent<Collider>());
        }

        private static void CreateZoneSign(Transform parent, string label, Vector3 position, Color color)
        {
            var sign = new GameObject($"{label} Sign");
            sign.transform.SetParent(parent, false);
            sign.transform.position = position;
            sign.transform.rotation = Quaternion.identity;

            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Plaque";
            backing.transform.SetParent(sign.transform, false);
            backing.transform.localScale = new Vector3(3.15f, 0.62f, 0.08f);
            backing.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial($"{label} Sign", color);
            UnityEngine.Object.DestroyImmediate(backing.GetComponent<Collider>());

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(sign.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.06f);
            var text = textObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = label.Length > 11 ? 0.078f : 0.1f;
            text.fontSize = 42;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
        }

        private static void CreateWall(Transform parent, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall Collider";
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Wall", WallColor);
        }

        private static GameObject AddScenePrefab(
            string path,
            Vector3 position,
            Quaternion rotation,
            string name,
            float scale = 1f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"Scene decoration was skipped because '{path}' was not found.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = Vector3.one * scale;
            MaterialRepairTool.ApplyKnownReplacements(instance);
            return instance;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static AudioClip LoadAudio(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"Audio clip not found at '{path}'. The associated cue will remain silent.");
            }

            return clip;
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
