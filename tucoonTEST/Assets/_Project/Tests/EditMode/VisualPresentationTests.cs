using NUnit.Framework;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Products;
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
    }
}
