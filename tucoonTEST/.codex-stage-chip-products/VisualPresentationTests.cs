using System.Linq;
using NUnit.Framework;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Products;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.Tests
{
    public sealed class VisualPresentationTests
    {
        [Test]
        public void ShelfProductSlot_MapsFillStatesToSixFourTwoZeroItems()
        {
            var slotObject = new GameObject("Slot");
            var definition = ScriptableObject.CreateInstance<ProductDefinition>();
            try
            {
                var visualRoot = new GameObject("Stock Group");
                visualRoot.transform.SetParent(slotObject.transform, false);
                var items = new GameObject[6];
                for (var i = 0; i < items.Length; i++)
                {
                    items[i] = new GameObject($"Item {i + 1}");
                    items[i].transform.SetParent(visualRoot.transform, false);
                }

                var slot = slotObject.AddComponent<ShelfProductSlot>();
                slot.Configure(definition, visualRoot, items);

                Assert.That(slot.VisibleItemCount, Is.EqualTo(6));
                slot.SetFillState(ProductStockFill.Medium);
                Assert.That(slot.VisibleItemCount, Is.EqualTo(4));
                slot.SetFillState(ProductStockFill.Low);
                Assert.That(slot.VisibleItemCount, Is.EqualTo(2));
                slot.SetFillState(ProductStockFill.Empty);
                Assert.That(slot.VisibleItemCount, Is.Zero);
                Assert.That(visualRoot.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(slotObject);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void BuildSpotView_PositionsBuiltChipAboveRenderedBuilding()
        {
            var spot = new GameObject("Spot");
            var prompt = new GameObject("Prompt", typeof(RectTransform));
            var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            try
            {
                prompt.transform.SetParent(spot.transform, false);
                var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(prompt.transform, false);
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);

                building.transform.SetParent(spot.transform, false);
                building.transform.localScale = new Vector3(2f, 2f, 2f);

                var view = prompt.AddComponent<BuildSpotView>();
                view.Configure(buttonObject.GetComponent<Button>(), labelObject.GetComponent<Text>(), prompt);
                view.ShowBuilt(definition, 1, building);

                Assert.That(prompt.transform.localPosition.y, Is.GreaterThan(2.4f));
                Assert.That(prompt.transform.localScale.x, Is.GreaterThan(0.009f));
                StringAssert.Contains("SHELF LV.1", labelObject.GetComponent<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(spot);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void GeneratedProductPrefabs_HaveExpectedGroupedStockTopology()
        {
            AssertShelfGroups("Assets/_Project/Art/Prefabs/Buildings/ShelfBuilding.prefab");
            AssertShelfGroups("Assets/_Project/Art/Prefabs/Buildings/PremiumShelfBuilding.prefab");

            var fresh = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Art/Prefabs/Environment/FreshMarketDisplay.prefab");
            Assert.That(fresh, Is.Not.Null);
            var freshChildren = fresh.GetComponentsInChildren<Transform>(true);
            Assert.That(freshChildren.Count(child => child.name.EndsWith("Produce Case")), Is.EqualTo(3));
            var produceGroups = freshChildren.Where(child => child.name.EndsWith("Produce Group")).ToArray();
            Assert.That(produceGroups, Has.Length.EqualTo(3));
            Assert.That(produceGroups.All(group => group.childCount == 6), Is.True);

            var checkout = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Art/Prefabs/Environment/CheckoutImpulseDisplay.prefab");
            Assert.That(checkout, Is.Not.Null);
            var impulseGroups = checkout.GetComponentsInChildren<Transform>(true)
                .Where(child => child.name.EndsWith("Impulse Group"))
                .ToArray();
            Assert.That(impulseGroups, Has.Length.EqualTo(2));
            Assert.That(impulseGroups.All(group => group.childCount == 4), Is.True);
        }

        [Test]
        public void GeneratedGameScene_HasNoMissingScriptsAndExpectedBuildSpots()
        {
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity", OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                var missingScriptCount = roots
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Sum(transform => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject));

                Assert.That(missingScriptCount, Is.Zero);
                Assert.That(
                    Object.FindObjectsByType<BuildSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                    Has.Length.EqualTo(6));

                var freshIsland = GameObject.Find("Fresh Food Island");
                Assert.That(freshIsland, Is.Not.Null);
                Assert.That(freshIsland.transform.position.x, Is.EqualTo(-7.2f).Within(0.01f));
                Assert.That(freshIsland.transform.position.z, Is.EqualTo(0.15f).Within(0.01f));
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static void AssertShelfGroups(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            var slots = prefab.GetComponentsInChildren<ShelfProductSlot>(true);
            Assert.That(slots, Has.Length.EqualTo(8), path);
            for (var i = 0; i < slots.Length; i++)
            {
                var serialized = new SerializedObject(slots[i]);
                Assert.That(serialized.FindProperty("visualItems").arraySize, Is.EqualTo(6), slots[i].name);
            }
        }
    }
}
