using System;
using SupermarketTycoon.Products;
using UnityEditor;
using UnityEngine;

namespace SupermarketTycoon.Editor
{
    public static partial class SupermarketTycoonDemoBuilder
    {
        private const string FoodModelRoot = Root + "/kennyfood/Models/FBX format";
        private const string FurnitureModelRoot = Root + "/kennyfurniture/Models/FBX format";
        private const string ProductDataRoot = DataRoot + "/Products";
        private const string ProductPrefabRoot = PrefabRoot + "/Products";
        private const string EnvironmentPrefabRoot = PrefabRoot + "/Environment";

        private const string PremiumShelfPrefabPath = PrefabRoot + "/Buildings/PremiumShelfBuilding.prefab";
        private const string StorageCornerPrefabPath = EnvironmentPrefabRoot + "/StorageCorner.prefab";
        private const string FreshDisplayPrefabPath = EnvironmentPrefabRoot + "/FreshMarketDisplay.prefab";
        private const string EntranceDecorPrefabPath = EnvironmentPrefabRoot + "/EntranceDecor.prefab";
        private const string CheckoutImpulsePrefabPath = EnvironmentPrefabRoot + "/CheckoutImpulseDisplay.prefab";

        private static readonly ProductSpec[] ProductSpecs =
        {
            new ProductSpec("Apple", "product.apple", "Apple", "apple.fbx", ProductCategory.Fruit, 10, 1f, 1, false, 0.34f),
            new ProductSpec("Banana", "product.banana", "Banana", "banana.fbx", ProductCategory.Fruit, 10, 1f, 1, false, 0.42f),
            new ProductSpec("Carrot", "product.carrot", "Carrot", "carrot.fbx", ProductCategory.Vegetables, 11, 1.05f, 1, false, 0.44f),
            new ProductSpec("Bread", "product.bread", "Bread", "bread.fbx", ProductCategory.Bread, 12, 1.1f, 1, false, 0.46f),
            new ProductSpec("SodaCan", "product.soda", "Soda", "soda-can.fbx", ProductCategory.Drinks, 13, 1.15f, 2, false, 0.4f),
            new ProductSpec("Carton", "product.carton", "Carton", "carton.fbx", ProductCategory.PackagedFood, 12, 1.1f, 2, false, 0.42f),
            new ProductSpec("Chocolate", "product.chocolate", "Chocolate", "chocolate-wrapper.fbx", ProductCategory.Snacks, 14, 1.25f, 2, false, 0.4f),
            new ProductSpec("Donut", "product.donut", "Donut", "donut-sprinkles.fbx", ProductCategory.Snacks, 14, 1.25f, 3, false, 0.34f),
            new ProductSpec("Pineapple", "product.pineapple", "Pineapple", "pineapple.fbx", ProductCategory.Premium, 20, 1.4f, 4, true, 0.54f),
            new ProductSpec("Wine", "product.wine", "Wine", "wine-red.fbx", ProductCategory.Premium, 24, 1.5f, 4, true, 0.52f)
        };

        private static ProductDefinition[] CreateProductAssets()
        {
            var products = new ProductDefinition[ProductSpecs.Length];
            for (var i = 0; i < ProductSpecs.Length; i++)
            {
                var spec = ProductSpecs[i];
                var prefabPath = $"{ProductPrefabRoot}/{spec.AssetName}Product.prefab";
                CreateProductPrefab(prefabPath, spec);

                var assetPath = $"{ProductDataRoot}/{spec.AssetName}Product.asset";
                var definition = AssetDatabase.LoadAssetAtPath<ProductDefinition>(assetPath);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<ProductDefinition>();
                    AssetDatabase.CreateAsset(definition, assetPath);
                }

                var serialized = new SerializedObject(definition);
                serialized.FindProperty("id").stringValue = spec.Id;
                serialized.FindProperty("displayName").stringValue = spec.DisplayName;
                serialized.FindProperty("visualPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                serialized.FindProperty("category").enumValueIndex = (int)spec.Category;
                serialized.FindProperty("baseValue").intValue = spec.BaseValue;
                serialized.FindProperty("valueMultiplier").floatValue = spec.ValueMultiplier;
                serialized.FindProperty("requiredStoreLevel").intValue = spec.RequiredLevel;
                serialized.FindProperty("premium").boolValue = spec.Premium;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
                products[i] = definition;
            }

            return products;
        }

        private static void CreateProductPrefab(string prefabPath, ProductSpec spec)
        {
            var root = new GameObject($"{spec.DisplayName} Product");
            try
            {
                var model = AddNestedVisual(root.transform, $"{FoodModelRoot}/{spec.SourceFile}", "Model");
                if (model == null)
                {
                    model = CreateFallbackPrimitive(
                        root.transform,
                        PrimitiveType.Sphere,
                        "Fallback Product",
                        Vector3.one * 0.32f,
                        AccentYellow);
                }
                else
                {
                    NormalizeVisualBounds(model, spec.ModelScale);
                }

                RemoveColliders(root);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static ShelfStockView CreateShelfStock(
            Transform root,
            ProductDefinition[] products,
            bool premium)
        {
            var stockRoot = new GameObject("Product Stock");
            stockRoot.transform.SetParent(root, false);
            var stock = stockRoot.AddComponent<ShelfStockView>();
            var slots = new ShelfProductSlot[8];
            var generalIndices = new[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            var premiumIndices = new[] { 8, 9, 6, 7, 8, 9, 6, 7 };
            var productIndices = premium ? premiumIndices : generalIndices;

            for (var i = 0; i < slots.Length; i++)
            {
                var slotObject = new GameObject($"Product Slot {i + 1:00}");
                slotObject.transform.SetParent(stockRoot.transform, false);
                var column = i % 4;
                var row = i / 4;
                slotObject.transform.localPosition = new Vector3(-1.08f + column * 0.72f, 0.66f + row * 0.66f, -0.2f);

                var definition = products[productIndices[i]];
                var visual = definition != null && definition.VisualPrefab != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(definition.VisualPrefab)
                    : null;
                if (visual != null)
                {
                    visual.name = definition.DisplayName;
                    visual.transform.SetParent(slotObject.transform, false);
                    visual.transform.localScale = Vector3.one * 0.48f;
                }

                slots[i] = slotObject.AddComponent<ShelfProductSlot>();
                slots[i].Configure(definition, visual);
            }

            stock.Configure(slots, premium ? 3.8f : 4.8f);
            return stock;
        }

        private static ProductCarryView CreateProductCarryView(Transform customerRoot, ProductDefinition[] products)
        {
            var anchor = new GameObject("Product Carry Anchor").transform;
            anchor.SetParent(customerRoot, false);
            anchor.localPosition = new Vector3(0.34f, 1.05f, 0.24f);
            anchor.localRotation = Quaternion.Euler(8f, -18f, 0f);

            var visuals = new CarryProductVisual[products.Length];
            for (var i = 0; i < products.Length; i++)
            {
                var definition = products[i];
                var visual = definition != null && definition.VisualPrefab != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(definition.VisualPrefab)
                    : null;
                if (visual != null)
                {
                    visual.name = $"Carry {definition.DisplayName}";
                    visual.transform.SetParent(anchor, false);
                    visual.transform.localScale = Vector3.one * 0.5f;
                    visual.SetActive(false);
                }

                visuals[i] = new CarryProductVisual(definition, visual);
            }

            var carryView = customerRoot.gameObject.AddComponent<ProductCarryView>();
            carryView.Configure(anchor, visuals);
            return carryView;
        }

        private static void CreateEnvironmentPrefabs(ProductDefinition[] products)
        {
            CreateCompositePrefab(StorageCornerPrefabPath, "Storage Corner", root =>
            {
                AddEnvironmentModel(root, "cardboardBoxClosed.fbx", "Closed Boxes", new Vector3(-0.8f, 0f, 0.2f), Quaternion.identity, 0.78f);
                AddEnvironmentModel(root, "cardboardBoxOpen.fbx", "Open Box", new Vector3(0.1f, 0f, 0.4f), Quaternion.Euler(0f, 30f, 0f), 0.78f);
                AddEnvironmentModel(root, "desk.fbx", "Staff Desk", new Vector3(1.15f, 0f, 0f), Quaternion.Euler(0f, 180f, 0f), 1.85f);
                AddEnvironmentModel(root, "chairDesk.fbx", "Staff Chair", new Vector3(1.1f, 0f, 0.75f), Quaternion.Euler(0f, 180f, 0f), 1.05f);
                AddEnvironmentModel(root, "computerScreen.fbx", "Office Screen", new Vector3(1.1f, 0.82f, -0.08f), Quaternion.Euler(0f, 180f, 0f), 0.48f);
                AddEnvironmentModel(root, "plantSmall2.fbx", "Storage Plant", new Vector3(-1.45f, 0f, 0.1f), Quaternion.identity, 0.9f);
            });

            CreateCompositePrefab(FreshDisplayPrefabPath, "Fresh Market Display", root =>
            {
                AddEnvironmentModel(root, "tableCross.fbx", "Produce Table", Vector3.zero, Quaternion.identity, 2.2f);
                var fresh = new[] { products[0], products[1], products[2], products[8] };
                for (var i = 0; i < fresh.Length; i++)
                {
                    AddProductDecor(root, fresh[i], new Vector3(-0.72f + i * 0.48f, 0.92f, 0f), 0.45f);
                }
            });

            CreateCompositePrefab(EntranceDecorPrefabPath, "Entrance Decor", root =>
            {
                AddEnvironmentModel(root, "rugDoormat.fbx", "Entry Mat", Vector3.zero, Quaternion.identity, 2.5f);
                AddEnvironmentModel(root, "bench.fbx", "Waiting Bench", new Vector3(-1.4f, 0f, 0.65f), Quaternion.Euler(0f, 90f, 0f), 2f);
                AddEnvironmentModel(root, "pottedPlant.fbx", "Entrance Plant", new Vector3(1.45f, 0f, 0.55f), Quaternion.identity, 1f);
            });

            CreateCompositePrefab(CheckoutImpulsePrefabPath, "Checkout Impulse Display", root =>
            {
                var stand = AddNestedVisual(root, SourceMiniShelfPath, "Impulse Stand");
                if (stand != null)
                {
                    stand.transform.localScale = Vector3.one * 0.72f;
                }

                AddProductDecor(root, products[6], new Vector3(-0.28f, 0.72f, -0.12f), 0.36f);
                AddProductDecor(root, products[7], new Vector3(0.25f, 0.72f, -0.12f), 0.36f);
            });
        }

        private static void CreateCompositePrefab(string path, string name, Action<Transform> compose)
        {
            var root = new GameObject(name);
            try
            {
                compose(root.transform);
                RemoveColliders(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject AddEnvironmentModel(
            Transform parent,
            string fileName,
            string name,
            Vector3 localPosition,
            Quaternion localRotation,
            float targetSize)
        {
            var model = AddNestedVisual(parent, $"{FurnitureModelRoot}/{fileName}", name);
            if (model != null)
            {
                model.transform.localPosition = localPosition;
                model.transform.localRotation = localRotation;
                NormalizeVisualBounds(model, targetSize);
            }

            return model;
        }

        private static void AddProductDecor(Transform parent, ProductDefinition definition, Vector3 position, float scale)
        {
            if (definition == null || definition.VisualPrefab == null)
            {
                return;
            }

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(definition.VisualPrefab);
            visual.name = definition.DisplayName;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = position;
            visual.transform.localScale = Vector3.one * scale;
        }

        private static void RemoveColliders(GameObject root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = colliders.Length - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(colliders[i]);
            }
        }

        private static void NormalizeVisualBounds(GameObject visual, float targetMaximumSize)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var maximumSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maximumSize > 0.0001f)
            {
                visual.transform.localScale *= targetMaximumSize / maximumSize;
            }
        }

        private readonly struct ProductSpec
        {
            public ProductSpec(
                string assetName,
                string id,
                string displayName,
                string sourceFile,
                ProductCategory category,
                int baseValue,
                float valueMultiplier,
                int requiredLevel,
                bool premium,
                float modelScale)
            {
                AssetName = assetName;
                Id = id;
                DisplayName = displayName;
                SourceFile = sourceFile;
                Category = category;
                BaseValue = baseValue;
                ValueMultiplier = valueMultiplier;
                RequiredLevel = requiredLevel;
                Premium = premium;
                ModelScale = modelScale;
            }

            public string AssetName { get; }
            public string Id { get; }
            public string DisplayName { get; }
            public string SourceFile { get; }
            public ProductCategory Category { get; }
            public int BaseValue { get; }
            public float ValueMultiplier { get; }
            public int RequiredLevel { get; }
            public bool Premium { get; }
            public float ModelScale { get; }
        }
    }
}
