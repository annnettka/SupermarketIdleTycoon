using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SupermarketTycoon.Editor
{
    public static class ProjectSetupTool
    {
        private const string MenuPath = "Tools/Supermarket Tycoon/Setup Project";

        private static readonly string[] ProjectFolders =
        {
            "Assets/_Project",
            "Assets/_Project/Art",
            "Assets/_Project/Art/Materials",
            "Assets/_Project/Art/Models",
            "Assets/_Project/Art/Prefabs",
            "Assets/_Project/Art/VFX",
            "Assets/_Project/Audio",
            "Assets/_Project/Audio/Music",
            "Assets/_Project/Audio/SFX",
            "Assets/_Project/Data",
            "Assets/_Project/Data/ScriptableObjects",
            "Assets/_Project/Fonts",
            "Assets/_Project/Scenes",
            "Assets/_Project/Scripts",
            "Assets/_Project/Scripts/Core",
            "Assets/_Project/Scripts/Economy",
            "Assets/_Project/Scripts/Building",
            "Assets/_Project/Scripts/AI",
            "Assets/_Project/Scripts/Progression",
            "Assets/_Project/Scripts/Save",
            "Assets/_Project/Scripts/UI",
            "Assets/_Project/Scripts/Utilities",
            "Assets/_Project/UI",
            "Assets/_Project/UI/Icons",
            "Assets/_Project/UI/Sprites",
            "Assets/_Project/UI/Prefabs",
            "Assets/_Project/Editor"
        };

        private static readonly SceneDefinition[] ProjectScenes =
        {
            new SceneDefinition("Bootstrap", "Assets/_Project/Scenes/Bootstrap.unity", SceneContent.Empty),
            new SceneDefinition("MainMenu", "Assets/_Project/Scenes/MainMenu.unity", SceneContent.MainMenu),
            new SceneDefinition("Game", "Assets/_Project/Scenes/Game.unity", SceneContent.Game)
        };

        [MenuItem(MenuPath)]
        public static void SetupProject()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Supermarket Tycoon setup cancelled because modified scenes were not saved.");
                return;
            }

            var activeScenePath = SceneManager.GetActiveScene().path;
            var report = new SetupReport();

            AssetDatabase.Refresh();

            EnsureProjectDirectories(report);
            AssetDatabase.Refresh();

            EnsureProjectScenes(report);
            EnsureBuildSettings(report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RestorePreviouslyOpenScene(activeScenePath);
            LogReport(report);
        }

        private static void EnsureProjectDirectories(SetupReport report)
        {
            foreach (var folder in ProjectFolders)
            {
                if (AssetDatabase.IsValidFolder(folder) || Directory.Exists(ToFullPath(folder)))
                {
                    report.ExistingFolders.Add(folder);
                    continue;
                }

                var separatorIndex = folder.LastIndexOf('/');
                if (separatorIndex < 0)
                {
                    report.Warnings.Add($"Cannot create folder without a parent path: {folder}");
                    continue;
                }

                var parentPath = folder.Substring(0, separatorIndex);
                var folderName = folder.Substring(separatorIndex + 1);

                if (!AssetDatabase.IsValidFolder(parentPath) && !Directory.Exists(ToFullPath(parentPath)))
                {
                    report.Warnings.Add($"Cannot create {folder} because parent folder is missing: {parentPath}");
                    continue;
                }

                var guid = AssetDatabase.CreateFolder(parentPath, folderName);
                if (string.IsNullOrEmpty(guid))
                {
                    report.Warnings.Add($"Unity did not create folder: {folder}");
                    continue;
                }

                report.CreatedFolders.Add(folder);
            }
        }

        private static void EnsureProjectScenes(SetupReport report)
        {
            foreach (var sceneDefinition in ProjectScenes)
            {
                if (File.Exists(ToFullPath(sceneDefinition.Path)))
                {
                    report.ExistingScenes.Add(sceneDefinition.Path);
                    continue;
                }

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                switch (sceneDefinition.Content)
                {
                    case SceneContent.Empty:
                        break;
                    case SceneContent.MainMenu:
                        CreateMainCamera(new Vector3(0f, 1f, -10f), Quaternion.identity);
                        break;
                    case SceneContent.Game:
                        CreateMainCamera(new Vector3(0f, 6f, -10f), Quaternion.Euler(30f, 0f, 0f));
                        CreateDirectionalLight();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (EditorSceneManager.SaveScene(scene, sceneDefinition.Path))
                {
                    report.CreatedScenes.Add(sceneDefinition.Path);
                    continue;
                }

                report.Warnings.Add($"Failed to save scene: {sceneDefinition.Path}");
            }
        }

        private static void EnsureBuildSettings(SetupReport report)
        {
            var requiredPaths = ProjectScenes.Select(scene => scene.Path).ToArray();
            var existingScenes = EditorBuildSettings.scenes;
            var updatedScenes = new List<EditorBuildSettingsScene>();

            foreach (var path in requiredPaths)
            {
                var existingScene = existingScenes.FirstOrDefault(scene =>
                    string.Equals(scene.path, path, StringComparison.OrdinalIgnoreCase));

                updatedScenes.Add(new EditorBuildSettingsScene(path, true));

                if (existingScene == null)
                {
                    report.AddedBuildScenes.Add(path);
                }
                else if (!existingScene.enabled)
                {
                    report.EnabledBuildScenes.Add(path);
                }
            }

            foreach (var existingScene in existingScenes)
            {
                if (requiredPaths.Any(path => string.Equals(path, existingScene.path, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                updatedScenes.Add(existingScene);
                report.PreservedBuildScenes.Add(existingScene.path);
            }

            if (BuildSettingsAreEquivalent(existingScenes, updatedScenes))
            {
                report.BuildSettingsAlreadyCorrect = true;
                return;
            }

            EditorBuildSettings.scenes = updatedScenes.ToArray();
            report.BuildSettingsUpdated = true;
        }

        private static void CreateMainCamera(Vector3 position, Quaternion rotation)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(position, rotation);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;

            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateDirectionalLight()
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
        }

        private static void RestorePreviouslyOpenScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath) || !File.Exists(ToFullPath(scenePath)))
            {
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static bool BuildSettingsAreEquivalent(
            IReadOnlyList<EditorBuildSettingsScene> currentScenes,
            IReadOnlyList<EditorBuildSettingsScene> updatedScenes)
        {
            if (currentScenes.Count != updatedScenes.Count)
            {
                return false;
            }

            for (var i = 0; i < currentScenes.Count; i++)
            {
                var current = currentScenes[i];
                var updated = updatedScenes[i];

                if (!string.Equals(current.path, updated.path, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (current.enabled != updated.enabled)
                {
                    return false;
                }
            }

            return true;
        }

        private static string ToFullPath(string assetPath)
        {
            if (string.Equals(assetPath, "Assets", StringComparison.OrdinalIgnoreCase))
            {
                return Application.dataPath;
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Path must start with Assets/: {assetPath}", nameof(assetPath));
            }

            var relativePath = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, relativePath);
        }

        private static void LogReport(SetupReport report)
        {
            Debug.Log(
                "Supermarket Tycoon project setup complete.\n" +
                FormatList("Created folders", report.CreatedFolders) +
                FormatList("Existing folders", report.ExistingFolders) +
                FormatList("Created scenes", report.CreatedScenes) +
                FormatList("Existing scenes", report.ExistingScenes) +
                FormatList("Added build scenes", report.AddedBuildScenes) +
                FormatList("Enabled build scenes", report.EnabledBuildScenes) +
                FormatList("Preserved existing build scenes", report.PreservedBuildScenes) +
                $"Build settings updated: {report.BuildSettingsUpdated}\n" +
                $"Build settings already correct: {report.BuildSettingsAlreadyCorrect}\n" +
                FormatList("Warnings", report.Warnings));
        }

        private static string FormatList(string title, IReadOnlyCollection<string> values)
        {
            return values.Count == 0
                ? $"{title}: none\n"
                : $"{title}:\n- {string.Join("\n- ", values)}\n";
        }

        private enum SceneContent
        {
            Empty,
            MainMenu,
            Game
        }

        private struct SceneDefinition
        {
            public SceneDefinition(string name, string path, SceneContent content)
            {
                Name = name;
                Path = path;
                Content = content;
            }

            public string Name { get; }
            public string Path { get; }
            public SceneContent Content { get; }
        }

        private sealed class SetupReport
        {
            public List<string> CreatedFolders { get; } = new List<string>();
            public List<string> ExistingFolders { get; } = new List<string>();
            public List<string> CreatedScenes { get; } = new List<string>();
            public List<string> ExistingScenes { get; } = new List<string>();
            public List<string> AddedBuildScenes { get; } = new List<string>();
            public List<string> EnabledBuildScenes { get; } = new List<string>();
            public List<string> PreservedBuildScenes { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
            public bool BuildSettingsUpdated { get; set; }
            public bool BuildSettingsAlreadyCorrect { get; set; }
        }
    }
}
