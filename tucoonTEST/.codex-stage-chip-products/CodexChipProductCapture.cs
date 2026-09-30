using System;
using System.IO;
using System.Linq;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Products;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SupermarketTycoon.Editor
{
    public static class CodexChipProductCapture
    {
        private const int Width = 1400;
        private const int Height = 900;
        private const int MinimumImageBytes = 20000;

        public static void InspectShelfMesh()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Gridness Studios/Grocery Store Pack Lite/Prefabs/Shelves/Shelf_Flat.prefab");
            var filter = prefab != null ? prefab.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null)
            {
                throw new MissingReferenceException("Shelf_Flat has no readable MeshFilter.");
            }

            var levels = filter.sharedMesh.vertices
                .GroupBy(vertex => Math.Round(vertex.y, 4))
                .OrderBy(group => group.Key)
                .Select(group => $"{group.Key:0.####} ({group.Count()})");
            Debug.Log($"Shelf mesh bounds: {filter.sharedMesh.bounds}. Y levels: {string.Join(", ", levels)}");
        }

        public static void RepairScriptMapAndBuild()
        {
            var scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/_Project" });
            for (var i = 0; i < scriptGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                AssetDatabase.ImportAsset(
                    path,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var probe = new GameObject("Script Map Probe");
            try
            {
                var script = MonoScript.FromMonoBehaviour(probe.AddComponent<BuildSpotView>());
                var scriptPath = AssetDatabase.GetAssetPath(script);
                if (string.IsNullOrEmpty(scriptPath))
                {
                    throw new InvalidOperationException("BuildSpotView still has no resolvable MonoScript asset.");
                }

                Debug.Log($"Verified runtime script mapping at '{scriptPath}'.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }

            SupermarketTycoonDemoBuilder.BuildPlayableDemoBatch();
        }

        public static void Run()
        {
            var output = Environment.GetEnvironmentVariable("CODEX_CAPTURE_OUTPUT");
            if (string.IsNullOrWhiteSpace(output))
            {
                throw new InvalidOperationException("CODEX_CAPTURE_OUTPUT is not set.");
            }

            output = Path.GetFullPath(output);
            Directory.CreateDirectory(output);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity", OpenSceneMode.Single);
            var camera = Camera.main;
            if (camera == null)
            {
                throw new MissingReferenceException("The Game scene has no Main Camera.");
            }

            var preparedCount = PrepareBuildSpots();
            if (preparedCount != 3)
            {
                throw new InvalidOperationException($"Expected three capture buildings, prepared {preparedCount}.");
            }

            Canvas.ForceUpdateCanvases();
            Capture(camera, new Vector3(-0.7f, 1.1f, -0.1f), 5.1f, Path.Combine(output, "chips-close.png"));

            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].renderMode == RenderMode.WorldSpace)
                {
                    canvases[i].gameObject.SetActive(false);
                }
            }

            Capture(camera, new Vector3(4.6f, 0.8f, -5.25f), 4.1f, Path.Combine(output, "checkout-close.png"));
            Capture(camera, new Vector3(-7.2f, 0.55f, 0.15f), 3.25f, Path.Combine(output, "produce-close.png"));
            Capture(camera, new Vector3(-4.8f, 1.05f, -1.5f), 3.45f, Path.Combine(output, "grocery-close.png"));
            Capture(camera, new Vector3(5.55f, 1.2f, 5.65f), 3.6f, Path.Combine(output, "premium-close.png"));
            Debug.Log($"Codex chip/product captures written to '{output}'.");
        }

        private static int PrepareBuildSpots()
        {
            var prepared = 0;
            var spots = UnityEngine.Object.FindObjectsByType<BuildSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < spots.Length; i++)
            {
                var spot = spots[i];
                var view = spot.GetComponentInChildren<BuildSpotView>(true);
                if (view == null || spot.Definition == null)
                {
                    continue;
                }

                var shouldBuild = spot.StableId == "spot.shelf.a" ||
                                  spot.StableId == "spot.checkout.a" ||
                                  spot.StableId == "spot.shelf.premium";
                if (!shouldBuild)
                {
                    if (spot.StableId == "spot.shelf.b")
                    {
                        view.ShowAvailable(spot.Definition, true);
                    }
                    else
                    {
                        view.SetVisible(false);
                    }

                    continue;
                }

                var building = (GameObject)PrefabUtility.InstantiatePrefab(spot.Definition.Prefab, spot.PlacementRoot);
                building.name = $"Capture {spot.Definition.DisplayName}";
                building.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                building.transform.localScale = Vector3.one;
                ConfigureStockStates(building);
                view.ShowBuilt(spot.Definition, spot.Definition.IsPremium ? 3 : 1, building);
                prepared++;
            }

            return prepared;
        }

        private static void ConfigureStockStates(GameObject building)
        {
            var slots = building.GetComponentsInChildren<ShelfProductSlot>(true)
                .OrderBy(slot => slot.name)
                .ToArray();
            var states = new[]
            {
                ProductStockFill.Full,
                ProductStockFill.Medium,
                ProductStockFill.Low,
                ProductStockFill.Empty
            };

            for (var i = 0; i < slots.Length; i++)
            {
                slots[i].SetUnlocked(true);
                slots[i].ResetState();
                slots[i].SetFillState(states[i % states.Length]);
            }
        }

        private static void Capture(Camera camera, Vector3 target, float orthographicSize, string path)
        {
            var originalTarget = camera.targetTexture;
            var originalPosition = camera.transform.position;
            var originalRotation = camera.transform.rotation;
            var originalSize = camera.orthographicSize;
            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                if (!renderTexture.Create())
                {
                    throw new InvalidOperationException("The validation render texture could not be created.");
                }

                camera.transform.position = target - originalRotation * Vector3.forward * 20f;
                camera.transform.rotation = originalRotation;
                camera.orthographic = true;
                camera.orthographicSize = orthographicSize;
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                if (new FileInfo(path).Length < MinimumImageBytes || IsVisuallyBlank(texture))
                {
                    throw new InvalidOperationException($"Capture '{path}' is blank.");
                }
            }
            finally
            {
                RenderTexture.active = null;
                camera.targetTexture = originalTarget;
                camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
                camera.orthographicSize = originalSize;
                UnityEngine.Object.DestroyImmediate(texture);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static bool IsVisuallyBlank(Texture2D capturedTexture)
        {
            var pixels = capturedTexture.GetPixels32();
            var minimum = 255;
            var maximum = 0;
            for (var i = 0; i < pixels.Length; i += 2048)
            {
                var luminance = (pixels[i].r + pixels[i].g + pixels[i].b) / 3;
                minimum = Mathf.Min(minimum, luminance);
                maximum = Mathf.Max(maximum, luminance);
            }

            return maximum - minimum < 12;
        }
    }
}
