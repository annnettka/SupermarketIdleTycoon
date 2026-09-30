using System;
using System.Collections.Generic;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Progression;

namespace SupermarketTycoon.Expansion
{
    public enum ExpansionPurchaseResult
    {
        Success,
        AlreadyPurchased,
        Locked,
        InsufficientFunds,
        InvalidConfiguration
    }

    public sealed class StoreExpansionService : IDisposable
    {
        private readonly IWallet wallet;
        private readonly ProgressionService progression;
        private readonly BuildingService buildings;
        private readonly AudioService audio;
        private readonly List<StoreExpansionSpot> spots = new List<StoreExpansionSpot>();
        // Keep IDs independent of scene objects so final saves remain valid during nondeterministic Unity teardown.
        private readonly HashSet<string> purchasedIds = new HashSet<string>(StringComparer.Ordinal);

        public StoreExpansionService(
            IWallet wallet,
            ProgressionService progression,
            BuildingService buildings,
            AudioService audio)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            this.audio = audio ?? throw new ArgumentNullException(nameof(audio));
            wallet.BalanceChanged += OnBalanceChanged;
        }

        public event Action StateChanged;
        public event Action<StoreExpansionDefinition> Purchased;

        public bool CanAfford(StoreExpansionDefinition definition)
        {
            return definition != null && wallet.CanSpend(definition.Cost);
        }

        public bool IsPurchased(string expansionId)
        {
            return !string.IsNullOrEmpty(expansionId) && purchasedIds.Contains(expansionId);
        }

        public void Register(StoreExpansionSpot spot)
        {
            if (spot == null || spot.Definition == null || spots.Contains(spot))
            {
                return;
            }

            spots.Add(spot);
            spot.Initialize(this, progression);
        }

        public void Restore(IList<string> purchasedIds)
        {
            this.purchasedIds.Clear();
            for (var i = 0; i < spots.Count; i++)
            {
                var spot = spots[i];
                var id = spot.Definition.Id;
                var purchased = purchasedIds != null && purchasedIds.Contains(id);
                if (purchased)
                {
                    this.purchasedIds.Add(id);
                }

                spot.SetPurchased(purchased);
                buildings.SetExpansionUnlocked(id, purchased);
            }

            StateChanged?.Invoke();
        }

        public ExpansionPurchaseResult TryPurchase(StoreExpansionSpot spot)
        {
            if (spot == null || spot.Definition == null)
            {
                return ExpansionPurchaseResult.InvalidConfiguration;
            }

            if (spot.IsPurchased)
            {
                return ExpansionPurchaseResult.AlreadyPurchased;
            }

            if (progression.CurrentLevel < spot.Definition.RequiredLevel)
            {
                return ExpansionPurchaseResult.Locked;
            }

            if (!wallet.TrySpend(spot.Definition.Cost))
            {
                return ExpansionPurchaseResult.InsufficientFunds;
            }

            spot.SetPurchased(true);
            purchasedIds.Add(spot.Definition.Id);
            buildings.SetExpansionUnlocked(spot.Definition.Id, true);
            audio.Play(GameSound.Build);
            Purchased?.Invoke(spot.Definition);
            StateChanged?.Invoke();
            return ExpansionPurchaseResult.Success;
        }

        public List<string> CapturePurchasedIds()
        {
            return new List<string>(purchasedIds);
        }

        public void Dispose()
        {
            wallet.BalanceChanged -= OnBalanceChanged;
        }

        private void OnBalanceChanged(int _)
        {
            StateChanged?.Invoke();
        }
    }
}
