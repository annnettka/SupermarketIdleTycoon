using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SupermarketTycoon.Buildings
{
    public sealed class BuildSpotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float ChipScale = 0.0094f;
        private const float HoverScaleMultiplier = 1.04f;
        private const float UnbuiltChipHeight = 1.48f;
        private const float BuiltChipClearance = 1.45f;

        [SerializeField] private Button buildButton;
        [SerializeField] private Text label;
        [SerializeField] private GameObject purchaseRoot;

        public Button BuildButton => buildButton;

        public void Configure(Button button, Text text, GameObject root)
        {
            buildButton = button;
            label = text;
            purchaseRoot = root;
        }

        public void SetVisible(bool visible)
        {
            purchaseRoot.SetActive(visible);
        }

        public void ShowAvailable(BuildingDefinition definition, bool affordable)
        {
            purchaseRoot.SetActive(true);
            PositionAtUnbuiltHeight();
            SetChipScale(false);
            buildButton.interactable = affordable;
            label.text = definition.IsPremium
                ? $"UNLOCK PREMIUM\n${definition.Cost}"
                : $"BUILD {GetCompactName(definition)}\n${definition.Cost}";
        }

        public void ShowLocked(BuildingDefinition definition)
        {
            purchaseRoot.SetActive(true);
            PositionAtUnbuiltHeight();
            SetChipScale(false);
            buildButton.interactable = false;
            label.text = definition.IsPremium
                ? $"PREMIUM LOCKED\nLEVEL {definition.RequiredLevel}"
                : $"{GetCompactName(definition)} LOCKED\nLEVEL {definition.RequiredLevel}";
        }

        public void ShowBuilt(BuildingDefinition definition, int level, GameObject building)
        {
            purchaseRoot.SetActive(true);
            PositionAboveBuilding(building);
            SetChipScale(false);
            buildButton.interactable = true;
            label.text = definition.IsPremium
                ? $"PREMIUM LV.{level}\nMANAGE"
                : $"{GetCompactName(definition)} LV.{level}\nMANAGE";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (purchaseRoot != null && purchaseRoot.activeInHierarchy)
            {
                SetChipScale(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetChipScale(false);
        }

        private void PositionAtUnbuiltHeight()
        {
            purchaseRoot.transform.localPosition = new Vector3(0f, UnbuiltChipHeight, 0f);
        }

        private void PositionAboveBuilding(GameObject building)
        {
            if (building == null)
            {
                PositionAtUnbuiltHeight();
                return;
            }

            var renderers = building.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                PositionAtUnbuiltHeight();
                return;
            }

            var hasBounds = false;
            var bounds = default(Bounds);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is ParticleSystemRenderer)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (!hasBounds)
            {
                PositionAtUnbuiltHeight();
                return;
            }

            var parent = purchaseRoot.transform.parent;
            var worldPosition = parent != null ? parent.position : purchaseRoot.transform.position;
            worldPosition.y = bounds.max.y + BuiltChipClearance;
            var localPosition = parent != null ? parent.InverseTransformPoint(worldPosition) : worldPosition;
            purchaseRoot.transform.localPosition = new Vector3(0f, localPosition.y, 0f);
        }

        private void SetChipScale(bool emphasized)
        {
            if (purchaseRoot == null)
            {
                return;
            }

            var scale = ChipScale * (emphasized ? HoverScaleMultiplier : 1f);
            purchaseRoot.transform.localScale = Vector3.one * scale;
        }

        private static string GetCompactName(BuildingDefinition definition)
        {
            if (definition.IsPremium)
            {
                return "PREMIUM";
            }

            return definition.Type switch
            {
                BuildingType.Shelf => "SHELF",
                BuildingType.Checkout => "CHECKOUT",
                _ => definition.DisplayName.ToUpperInvariant()
            };
        }
    }
}
