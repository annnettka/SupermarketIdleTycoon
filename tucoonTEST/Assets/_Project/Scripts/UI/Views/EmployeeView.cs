using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    public sealed class EmployeeView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Text purchaseButtonLabel;

        public Button PurchaseButton => purchaseButton;

        public void Configure(GameObject panelRoot, Text title, Text status, Button purchase, Text purchaseLabel)
        {
            root = panelRoot;
            titleLabel = title;
            statusLabel = status;
            purchaseButton = purchase;
            purchaseButtonLabel = purchaseLabel;
        }

        public void Show(string title, string status, string buttonText, bool interactable)
        {
            root.SetActive(true);
            titleLabel.text = title;
            statusLabel.text = status;
            purchaseButtonLabel.text = buttonText;
            purchaseButton.interactable = interactable;
        }
    }
}
