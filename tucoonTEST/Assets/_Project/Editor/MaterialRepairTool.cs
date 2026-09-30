using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SupermarketTycoon.Editor
{
    public static class MaterialRepairTool
    {
        private const string DiagnoseMenuPath = "Tools/Supermarket Tycoon/Diagnose Materials";
        private const string RepairMenuPath = "Tools/Supermarket Tycoon/Repair Project-Owned Materials";
        private const string AutoRepairSessionKey = "SupermarketTycoon.MaterialRepair.AutoRepair.1";

        private const string ProjectRoot = "Assets/_Project";
        private const string ProjectPrefabRoot = ProjectRoot + "/Art/Prefabs";
        private const string MaterialRoot = ProjectRoot + "/Art/Materials";
        private const string GroceryMaterialRoot = MaterialRoot + "/GroceryStore";
        private const string VfxMaterialRoot = MaterialRoot + "/VFX";
        private const string MainMenuScenePath = ProjectRoot + "/Scenes/MainMenu.unity";
        private const string GameScenePath = ProjectRoot + "/Scenes/Game.unity";

        private static readonly MaterialDefinition[] Definitions =
        {
            new MaterialDefinition(
                "Assets/Gridness Studios/Grocery Store Pack Lite/Materials/Grossery_Mat.mat",
                GroceryMaterialRoot + "/Grossery_Mat_URP_Fixed.mat",
                "Universal Render Pipeline/Lit",
                false,
                false),
            new MaterialDefinition(
                "Assets/Gridness Studios/Grocery Store Pack Lite/Materials/Grocery_Light_Mat.mat",
                GroceryMaterialRoot + "/Grocery_Light_Mat_URP_Fixed.mat",
                "Universal Render Pipeline/Lit",
                true,
                false),
            new MaterialDefinition(
                "Assets/SimpleFX/Materials/FX_Glow.mat",
                VfxMaterialRoot + "/FX_Glow_URP_Fixed.mat",
                "Universal Render Pipeline/Particles/Unlit",
                false,
                true)
        };

        [InitializeOnLoadMethod]
        private static void ScheduleAutomaticRepair()
        {
            if (Application.isBatchMode || SessionState.GetBool(AutoRepairSessionKey, false))
            {
                return;
            }

            EditorApplication.delayCall += RunAutomaticRepair;
        }

        [MenuItem(DiagnoseMenuPath)]
        public static void DiagnoseMaterials()
        {
            var brokenCount = DiagnoseTargets(true);
            Debug.Log($"[MaterialRepair] Diagnosis complete. Broken material assignments: {brokenCount}.");
        }

        [MenuItem(RepairMenuPath)]
        public static void RepairMaterialsFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[MaterialRepair] Repair cancelled because modified scenes were not saved.");
                return;
            }

            RepairProjectOwnedMaterials(true);
        }

        public static void RepairProjectOwnedMaterialsBatch()
        {
            RepairProjectOwnedMaterials(true);
        }

        public static void RepairAndCaptureGameSceneBatch()
        {
            RepairProjectOwnedMaterials(true);
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var camera = FindSceneCamera(scene);
            if (camera == null)
            {
                throw new InvalidOperationException("The Game scene has no Camera for material validation.");
            }

            const int width = 1280;
            const int height = 720;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                screenshot.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                screenshot.Apply(false);

                var absolutePath = Path.GetFullPath(
                    Path.Combine(Application.dataPath, "../Logs/MaterialRepairGame.png"));
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllBytes(absolutePath, screenshot.EncodeToPNG());

                var pixels = screenshot.GetPixels32();
                var errorMagentaPixels = 0;
                for (var i = 0; i < pixels.Length; i++)
                {
                    var pixel = pixels[i];
                    if (pixel.r >= 245 && pixel.g <= 20 && pixel.b >= 245)
                    {
                        errorMagentaPixels++;
                    }
                }

                Debug.Log(
                    $"[MaterialRepair] Captured Game camera to '{absolutePath}'. " +
                    $"Error-magenta pixels: {errorMagentaPixels}/{pixels.Length}.");

                if (errorMagentaPixels > 0)
                {
                    throw new InvalidOperationException(
                        $"Rendered validation still contains {errorMagentaPixels} error-magenta pixels.");
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(screenshot);
            }
        }

        public static void EnsureFixedMaterials()
        {
            EnsureFolder(GroceryMaterialRoot);
            EnsureFolder(VfxMaterialRoot);

            for (var i = 0; i < Definitions.Length; i++)
            {
                CreateOrUpdateFixedMaterial(Definitions[i]);
            }

            AssetDatabase.SaveAssets();
        }

        public static int ApplyKnownReplacements(GameObject root)
        {
            if (root == null)
            {
                return 0;
            }

            var replacements = LoadReplacementMap();
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var changedCount = 0;

            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                var materials = renderer.sharedMaterials;
                var changed = false;

                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    var material = materials[materialIndex];
                    var sourcePath = material != null ? AssetDatabase.GetAssetPath(material) : string.Empty;
                    if (!replacements.TryGetValue(sourcePath, out var replacement) || replacement == null)
                    {
                        continue;
                    }

                    materials[materialIndex] = replacement;
                    changed = true;
                    changedCount++;
                }

                if (!changed)
                {
                    continue;
                }

                renderer.sharedMaterials = materials;
                if (PrefabUtility.IsPartOfPrefabInstance(renderer))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }

                EditorUtility.SetDirty(renderer);
            }

            return changedCount;
        }

        private static void RunAutomaticRepair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            SessionState.SetBool(AutoRepairSessionKey, true);
            RepairProjectOwnedMaterials(true);
        }

        private static void RepairProjectOwnedMaterials(bool logDiagnostics)
        {
            var before = DiagnoseTargets(logDiagnostics);
            EnsureFixedMaterials();
            var changedPrefabs = RepairProjectOwnedPrefabs();
            var changedScenes = RepairScene(MainMenuScenePath) + RepairScene(GameScenePath);
            AssetDatabase.SaveAssets();
            var remaining = DiagnoseTargets(logDiagnostics);

            if (remaining == 0)
            {
                Debug.Log(
                    $"[MaterialRepair] Repair complete. " +
                    $"Broken before: {before}, prefab assignments repaired: {changedPrefabs}, " +
                    $"scene assignments repaired: {changedScenes}, broken remaining: 0.");
            }
            else
            {
                Debug.LogError($"[MaterialRepair] Repair finished with {remaining} broken material assignments remaining.");
            }
        }

        private static int RepairProjectOwnedPrefabs()
        {
            var changedCount = 0;
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectPrefabRoot });

            for (var i = 0; i < prefabGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var changed = ApplyKnownReplacements(root);
                    if (changed <= 0)
                    {
                        continue;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changedCount += changed;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return changedCount;
        }

        private static int RepairScene(string path)
        {
            var previousActive = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(path);
            var closeAfterRepair = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                closeAfterRepair = true;
            }

            var changedCount = 0;
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                changedCount += ApplyKnownReplacements(roots[i]);
            }

            if (changedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (closeAfterRepair)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            if (previousActive.IsValid() && previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(previousActive);
            }

            return changedCount;
        }

        private static int DiagnoseTargets(bool logDetails)
        {
            var count = DiagnoseScene(MainMenuScenePath, logDetails) + DiagnoseScene(GameScenePath, logDetails);
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectPrefabRoot });

            for (var i = 0; i < prefabGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    count += DiagnoseRoot(root, $"Prefab: {path}", logDetails);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return count;
        }

        private static int DiagnoseScene(string path, bool logDetails)
        {
            var previousActive = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(path);
            var closeAfterDiagnosis = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                closeAfterDiagnosis = true;
            }

            var count = 0;
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                count += DiagnoseRoot(roots[i], $"Scene: {path}", logDetails);
            }

            if (closeAfterDiagnosis)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            if (previousActive.IsValid() && previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(previousActive);
            }

            return count;
        }

        private static int DiagnoseRoot(GameObject root, string owner, bool logDetails)
        {
            var brokenCount = 0;
            var renderers = root.GetComponentsInChildren<Renderer>(true);

            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                var materials = renderer.sharedMaterials;

                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    var material = materials[materialIndex];
                    if (!IsBrokenMaterial(material))
                    {
                        continue;
                    }

                    brokenCount++;
                    if (!logDetails)
                    {
                        continue;
                    }

                    var materialName = material != null ? material.name : "<missing>";
                    var shaderName = material != null && material.shader != null
                        ? material.shader.name
                        : "<missing>";
                    var sourcePath = material != null ? AssetDatabase.GetAssetPath(material) : "<missing>";
                    Debug.LogWarning(
                        $"[MaterialRepair] {owner} | Object: {GetHierarchyPath(renderer.transform)} | " +
                        $"Renderer: {renderer.GetType().Name} | Material: {materialName} | " +
                        $"Shader: {shaderName} | Source: {sourcePath}",
                        renderer);
                }
            }

            return brokenCount;
        }

        private static bool IsBrokenMaterial(Material material)
        {
            if (material == null || material.shader == null)
            {
                return true;
            }

            var sourcePath = AssetDatabase.GetAssetPath(material);
            for (var i = 0; i < Definitions.Length; i++)
            {
                if (string.Equals(sourcePath, Definitions[i].SourcePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var shaderName = material.shader.name;
            if (string.Equals(shaderName, "Hidden/InternalErrorShader", StringComparison.Ordinal) ||
                !material.shader.isSupported)
            {
                return true;
            }

            if (GraphicsSettings.currentRenderPipeline != null &&
                (string.Equals(shaderName, "Standard", StringComparison.Ordinal) ||
                 shaderName.StartsWith("Legacy Shaders/", StringComparison.Ordinal) ||
                 shaderName.StartsWith("Particles/", StringComparison.Ordinal)))
            {
                return true;
            }

            return false;
        }

        private static Dictionary<string, Material> LoadReplacementMap()
        {
            var replacements = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Definitions.Length; i++)
            {
                replacements[Definitions[i].SourcePath] =
                    AssetDatabase.LoadAssetAtPath<Material>(Definitions[i].TargetPath);
            }

            return replacements;
        }

        private static void CreateOrUpdateFixedMaterial(MaterialDefinition definition)
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(definition.SourcePath);
            if (source == null)
            {
                Debug.LogWarning($"[MaterialRepair] Source material was not found: {definition.SourcePath}");
                return;
            }

            var shader = Shader.Find(definition.TargetShaderName);
            if (shader == null && definition.IsParticle)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"Required URP shader '{definition.TargetShaderName}' was not found.");
            }

            var target = AssetDatabase.LoadAssetAtPath<Material>(definition.TargetPath);
            if (target == null)
            {
                target = new Material(shader)
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(definition.TargetPath)
                };
                AssetDatabase.CreateAsset(target, definition.TargetPath);
            }
            else if (target.shader != shader)
            {
                target.shader = shader;
            }

            CopyMaterialProperties(source, target, definition);
            EditorUtility.SetDirty(target);
        }

        private static void CopyMaterialProperties(
            Material source,
            Material target,
            MaterialDefinition definition)
        {
            var baseTextureProperty = FindTextureProperty(source, "_BaseMap", "_MainTex");
            CopyTexture(source, target, baseTextureProperty, "_BaseMap", "_MainTex");

            var baseColor = definition.IsParticle
                ? ReadColor(source, Color.white, "_TintColor", "_BaseColor", "_Color")
                : ReadColor(source, Color.white, "_BaseColor", "_Color");
            SetColor(target, baseColor, "_BaseColor", "_Color");

            CopyTexture(source, target, FindTextureProperty(source, "_BumpMap", "_NormalMap"), "_BumpMap");
            SetFloat(target, ReadFloat(source, 1f, "_BumpScale"), "_BumpScale");
            SetKeyword(target, "_NORMALMAP", target.HasProperty("_BumpMap") && target.GetTexture("_BumpMap") != null);

            CopyTexture(
                source,
                target,
                FindTextureProperty(source, "_MetallicGlossMap"),
                "_MetallicGlossMap");
            SetFloat(target, ReadFloat(source, 0f, "_Metallic"), "_Metallic");
            SetFloat(target, ReadFloat(source, 0.5f, "_Smoothness", "_Glossiness"), "_Smoothness", "_Glossiness");
            SetKeyword(
                target,
                "_METALLICSPECGLOSSMAP",
                target.HasProperty("_MetallicGlossMap") && target.GetTexture("_MetallicGlossMap") != null);

            CopyTexture(source, target, FindTextureProperty(source, "_OcclusionMap"), "_OcclusionMap");
            SetFloat(target, ReadFloat(source, 1f, "_OcclusionStrength"), "_OcclusionStrength");
            SetKeyword(target, "_OCCLUSIONMAP", target.HasProperty("_OcclusionMap") && target.GetTexture("_OcclusionMap") != null);

            var emissionEnabled = definition.Emissive || source.IsKeywordEnabled("_EMISSION");
            if (emissionEnabled)
            {
                var emissionProperty = FindTextureProperty(source, "_EmissionMap");
                CopyTexture(source, target, emissionProperty, "_EmissionMap");
                SetColor(
                    target,
                    ReadColor(source, Color.white, "_EmissionColor", "_EmisColor"),
                    "_EmissionColor");
            }
            else
            {
                SetColor(target, Color.black, "_EmissionColor");
            }

            SetKeyword(target, "_EMISSION", emissionEnabled);
            target.globalIlluminationFlags = emissionEnabled
                ? source.globalIlluminationFlags
                : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            target.enableInstancing = source.enableInstancing;
            target.doubleSidedGI = source.doubleSidedGI;

            if (definition.IsParticle)
            {
                ConfigureTransparentParticle(target);
            }
            else
            {
                ConfigureLitSurface(source, target, baseColor.a);
            }
        }

        private static void ConfigureLitSurface(Material source, Material target, float alpha)
        {
            var sourceMode = Mathf.RoundToInt(ReadFloat(source, 0f, "_Surface", "_Mode"));
            var alphaClip = sourceMode == 1;
            var transparent = sourceMode >= 2 || alpha < 0.999f;

            SetFloat(target, transparent ? 1f : 0f, "_Surface");
            SetFloat(target, alphaClip ? 1f : 0f, "_AlphaClip");
            SetFloat(target, ReadFloat(source, 0.5f, "_Cutoff"), "_Cutoff");
            SetKeyword(target, "_ALPHATEST_ON", alphaClip);
            SetKeyword(target, "_SURFACE_TYPE_TRANSPARENT", transparent);

            if (transparent)
            {
                target.SetOverrideTag("RenderType", "Transparent");
                SetFloat(target, (float)BlendMode.SrcAlpha, "_SrcBlend");
                SetFloat(target, (float)BlendMode.OneMinusSrcAlpha, "_DstBlend");
                SetFloat(target, 0f, "_ZWrite");
                target.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                target.SetOverrideTag("RenderType", alphaClip ? "TransparentCutout" : "Opaque");
                SetFloat(target, (float)BlendMode.One, "_SrcBlend");
                SetFloat(target, (float)BlendMode.Zero, "_DstBlend");
                SetFloat(target, 1f, "_ZWrite");
                target.renderQueue = alphaClip ? (int)RenderQueue.AlphaTest : -1;
            }
        }

        private static void ConfigureTransparentParticle(Material target)
        {
            target.SetOverrideTag("RenderType", "Transparent");
            SetFloat(target, 1f, "_Surface");
            SetFloat(target, 2f, "_Blend");
            SetFloat(target, (float)BlendMode.SrcAlpha, "_SrcBlend");
            SetFloat(target, (float)BlendMode.One, "_DstBlend");
            SetFloat(target, 0f, "_ZWrite");
            target.renderQueue = (int)RenderQueue.Transparent;
            SetKeyword(target, "_SURFACE_TYPE_TRANSPARENT", true);
            SetKeyword(target, "_ALPHAPREMULTIPLY_ON", false);
        }

        private static string FindTextureProperty(Material material, params string[] properties)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                if (material.HasProperty(properties[i]))
                {
                    return properties[i];
                }
            }

            return null;
        }

        private static void CopyTexture(
            Material source,
            Material target,
            string sourceProperty,
            params string[] targetProperties)
        {
            if (string.IsNullOrEmpty(sourceProperty))
            {
                return;
            }

            var texture = source.GetTexture(sourceProperty);
            var scale = source.GetTextureScale(sourceProperty);
            var offset = source.GetTextureOffset(sourceProperty);

            for (var i = 0; i < targetProperties.Length; i++)
            {
                var property = targetProperties[i];
                if (!target.HasProperty(property))
                {
                    continue;
                }

                target.SetTexture(property, texture);
                target.SetTextureScale(property, scale);
                target.SetTextureOffset(property, offset);
            }
        }

        private static Color ReadColor(Material material, Color fallback, params string[] properties)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                if (material.HasProperty(properties[i]))
                {
                    return material.GetColor(properties[i]);
                }
            }

            return fallback;
        }

        private static float ReadFloat(Material material, float fallback, params string[] properties)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                if (material.HasProperty(properties[i]))
                {
                    return material.GetFloat(properties[i]);
                }
            }

            return fallback;
        }

        private static void SetColor(Material material, Color value, params string[] properties)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                if (material.HasProperty(properties[i]))
                {
                    material.SetColor(properties[i], value);
                }
            }
        }

        private static void SetFloat(Material material, float value, params string[] properties)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                if (material.HasProperty(properties[i]))
                {
                    material.SetFloat(properties[i], value);
                }
            }
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        private static Camera FindSceneCamera(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var camera = roots[i].GetComponentInChildren<Camera>(true);
                if (camera != null)
                {
                    return camera;
                }
            }

            return null;
        }

        private static void EnsureFolder(string folder)
        {
            var parts = folder.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private sealed class MaterialDefinition
        {
            public MaterialDefinition(
                string sourcePath,
                string targetPath,
                string targetShaderName,
                bool emissive,
                bool isParticle)
            {
                SourcePath = sourcePath;
                TargetPath = targetPath;
                TargetShaderName = targetShaderName;
                Emissive = emissive;
                IsParticle = isParticle;
            }

            public string SourcePath { get; }
            public string TargetPath { get; }
            public string TargetShaderName { get; }
            public bool Emissive { get; }
            public bool IsParticle { get; }
        }
    }
}
