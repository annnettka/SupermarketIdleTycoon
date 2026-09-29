using System;
using System.IO;
using SupermarketTycoon.Bootstrap;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Core;
using SupermarketTycoon.Customers;
using SupermarketTycoon.Progression;
using SupermarketTycoon.SceneFlow;
using SupermarketTycoon.UI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SupermarketTycoon.Editor
{
    public static partial class SupermarketTycoonDemoBuilder
    {
        private static void BuildBootstrapScene(GameConfig gameConfig)
        {
            BuildOwnedScene(BootstrapScenePath, () =>
            {
                var root = new GameObject("Application");
                var bootstrapper = root.AddComponent<AppBootstrapper>();

                var musicSource = root.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                var sfxSource = root.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;

                var loading = CreateLoadingScreen(root.transform);
                SetReference(bootstrapper, "gameConfig", gameConfig);
                SetReference(bootstrapper, "loadingScreen", loading);
                SetReference(bootstrapper, "musicSource", musicSource);
                SetReference(bootstrapper, "sfxSource", sfxSource);
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
                var camera = CreateIsometricCamera(new Vector3(10f, 9f, -12f), new Vector3(0f, 1f, 0f), false);
                camera.fieldOfView = 42f;
                CreateLighting();

                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Menu Floor";
                floor.transform.position = new Vector3(0f, -0.15f, 0f);
                floor.transform.localScale = new Vector3(14f, 0.3f, 10f);
                floor.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Floor", FloorColor);

                AddScenePrefab(SourceShelfPath, new Vector3(-2.4f, 0f, 1f), Quaternion.Euler(0f, 15f, 0f), "Shelf Display");
                AddScenePrefab(SourceCheckoutPath, new Vector3(2.5f, 0f, -0.5f), Quaternion.Euler(0f, -10f, 0f), "Checkout Display");
                AddScenePrefab(SourceCharacterPath, new Vector3(0.5f, 0f, 1.8f), Quaternion.Euler(0f, 180f, 0f), "Customer Display", 0.82f);

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
            BuildingDefinition checkoutExpansion)
        {
            BuildOwnedScene(GameScenePath, () =>
            {
                var camera = CreateIsometricCamera(new Vector3(11.5f, 15.5f, -15.5f), new Vector3(0f, 0f, 0f), true);
                camera.orthographicSize = 10.2f;
                CreateLighting();
                CreateStoreEnvironment();
                EnsureEventSystem();

                var spots = new[]
                {
                    CreateWorldBuildSpot("spot.shelf.a", shelf, new Vector3(-3.2f, 0.12f, 1.8f), Quaternion.Euler(0f, 0f, 0f), camera),
                    CreateWorldBuildSpot("spot.checkout.a", checkout, new Vector3(3.2f, 0.12f, -1.7f), Quaternion.Euler(0f, 180f, 0f), camera),
                    CreateWorldBuildSpot("spot.shelf.b", shelfExpansion, new Vector3(-3.2f, 0.12f, -2.3f), Quaternion.Euler(0f, 0f, 0f), camera),
                    CreateWorldBuildSpot("spot.checkout.b", checkoutExpansion, new Vector3(3.2f, 0.12f, 2.4f), Quaternion.Euler(0f, 180f, 0f), camera)
                };

                var navigationRoot = new GameObject("Navigation");
                var surface = navigationRoot.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.layerMask = ~0;

                var customerSystem = new GameObject("Customer System");
                var spawn = new GameObject("Customer Spawn").transform;
                spawn.SetParent(customerSystem.transform, false);
                spawn.position = new Vector3(-1.2f, 0f, -7f);
                spawn.rotation = Quaternion.identity;
                var exit = new GameObject("Customer Exit").transform;
                exit.SetParent(customerSystem.transform, false);
                exit.position = new Vector3(1.3f, 0f, -7f);
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
                    out var pauseMenu,
                    out var settingsView);

                var entry = new GameObject("Game Entry Point").AddComponent<GameSceneEntryPoint>();
                entry.Configure(
                    gameConfig,
                    customerConfig,
                    progressionConfig,
                    spots,
                    spawner,
                    hud,
                    pauseMenu,
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
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.64f, 0.68f);
        }

        private static void CreateStoreEnvironment()
        {
            var environment = new GameObject("Store Environment").transform;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Walkable Floor";
            floor.transform.SetParent(environment, false);
            floor.transform.position = new Vector3(0f, -0.18f, -0.5f);
            floor.transform.localScale = new Vector3(15f, 0.35f, 15f);
            floor.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Floor", FloorColor);

            for (var x = -1; x <= 1; x++)
            {
                for (var z = -1; z <= 1; z++)
                {
                    AddScenePrefab(
                        SourceFloorPath,
                        new Vector3(x * 4f, 0.02f, z * 4f),
                        Quaternion.identity,
                        $"Floor Tile {x} {z}");
                }
            }

            CreateWall(environment, new Vector3(0f, 1.3f, 6.8f), new Vector3(15f, 2.6f, 0.3f));
            CreateWall(environment, new Vector3(-7.35f, 1.3f, -0.4f), new Vector3(0.3f, 2.6f, 14f));
            CreateWall(environment, new Vector3(7.35f, 1.3f, -0.4f), new Vector3(0.3f, 2.6f, 14f));
            CreateWall(environment, new Vector3(-4.7f, 1.3f, -7.1f), new Vector3(5.2f, 2.6f, 0.3f));
            CreateWall(environment, new Vector3(4.7f, 1.3f, -7.1f), new Vector3(5.2f, 2.6f, 0.3f));

            AddScenePrefab(SourceWallPath, new Vector3(0f, 0f, 6.55f), Quaternion.identity, "Grocery Rear Wall");
            AddScenePrefab(SourceDoorWallPath, new Vector3(0f, 0f, -6.75f), Quaternion.Euler(0f, 180f, 0f), "Grocery Entrance");

            var entranceSign = new GameObject("Entrance Sign");
            entranceSign.transform.SetParent(environment, false);
            entranceSign.transform.position = new Vector3(0f, 2.8f, -6.8f);
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
