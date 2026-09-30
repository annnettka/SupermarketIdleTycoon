using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.Buildings
{
    public sealed class BuildSpotView : MonoBehaviour
    {
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
            buildButton.interactable = affordable;
            label.text = $"BUILD {definition.DisplayName.ToUpperInvariant()}\n${definition.Cost}";
        }

        public void ShowLocked(int requiredLevel)
        {
            purchaseRoot.SetActive(true);
            buildButton.interactable = false;
            label.text = $"LOCKED\nLEVEL {requiredLevel}";
        }

        public void ShowBuilt(BuildingDefinition definition, int level)
        {
            purchaseRoot.SetActive(true);
            buildButton.interactable = true;
            label.text = $"{definition.DisplayName.ToUpperInvariant()}  |  LEVEL {level}\nMANAGE";
        }
    }
}
