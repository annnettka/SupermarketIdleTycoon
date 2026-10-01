using System;
using SupermarketTycoon.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.Expansion
{
    /// <summary>
    /// Adapts an authored expansion area to purchase input, lock presentation, and restored ownership state.
    /// Адаптирует подготовленную зону расширения к покупке, отображению блокировки и восстановленному владению.
    /// </summary>
    public sealed class StoreExpansionSpot : MonoBehaviour
    {
        [SerializeField] private StoreExpansionDefinition definition;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Text label;
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private GameObject unlockedVisual;

        private StoreExpansionService service;
        private ProgressionService progression;

        public StoreExpansionDefinition Definition => definition;
        public bool IsPurchased { get; private set; }

        public void Configure(
            StoreExpansionDefinition expansion,
            Button button,
            Text text,
            GameObject locked,
            GameObject unlocked)
        {
            definition = expansion;
            purchaseButton = button;
            label = text;
            lockedVisual = locked;
            unlockedVisual = unlocked;
        }

        public void Initialize(StoreExpansionService expansionService, ProgressionService progressionService)
        {
            service = expansionService ?? throw new ArgumentNullException(nameof(expansionService));
            progression = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
            purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
            progression.LevelChanged += OnLevelChanged;
            service.StateChanged += Refresh;
            Refresh();
        }

        public void SetPurchased(bool purchased)
        {
            IsPurchased = purchased;
            Refresh();
        }

        private void OnDestroy()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
            }

            if (progression != null)
            {
                progression.LevelChanged -= OnLevelChanged;
            }

            if (service != null)
            {
                service.StateChanged -= Refresh;
            }
        }

        private void OnPurchaseClicked()
        {
            service.TryPurchase(this);
        }

        private void OnLevelChanged(int _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (definition == null || purchaseButton == null || label == null || progression == null)
            {
                return;
            }

            if (lockedVisual != null)
            {
                lockedVisual.SetActive(!IsPurchased);
            }

            if (unlockedVisual != null)
            {
                unlockedVisual.SetActive(IsPurchased);
            }

            purchaseButton.gameObject.SetActive(!IsPurchased);
            if (IsPurchased)
            {
                return;
            }

            var levelLocked = progression.CurrentLevel < definition.RequiredLevel;
            purchaseButton.interactable = !levelLocked && service.CanAfford(definition);
            label.text = levelLocked
                ? $"STORE EXPANSION\nREQUIRES LEVEL {definition.RequiredLevel}"
                : $"BUILD EXPANSION\n${definition.Cost}";
        }
    }
}
