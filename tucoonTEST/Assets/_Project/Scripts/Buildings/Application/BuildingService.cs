using System;
using System.Collections.Generic;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Checkout;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Progression;
using SupermarketTycoon.Save;
using UnityEngine;

namespace SupermarketTycoon.Buildings
{
    public enum PurchaseResult
    {
        Success,
        AlreadyBuilt,
        Locked,
        InsufficientFunds,
        InvalidConfiguration
    }

    public sealed class BuildingService : IDisposable
    {
        private readonly IWallet wallet;
        private readonly ProgressionService progression;
        private readonly StationRegistry stations;
        private readonly AudioService audio;
        private readonly Dictionary<string, BuildSpot> spots = new Dictionary<string, BuildSpot>();

        public BuildingService(
            IWallet wallet,
            ProgressionService progression,
            StationRegistry stations,
            AudioService audio)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.stations = stations ?? throw new ArgumentNullException(nameof(stations));
            this.audio = audio ?? throw new ArgumentNullException(nameof(audio));
            wallet.BalanceChanged += OnStateChanged;
            progression.LevelChanged += OnLevelChanged;
        }

        public event Action StateChanged;
        public event Action<BuildSpot, BuildingDefinition> BuildingBuilt;

        public bool CanAfford(BuildingDefinition definition)
        {
            return definition != null && wallet.CanSpend(definition.Cost);
        }

        public void Register(BuildSpot spot)
        {
            if (spot == null || string.IsNullOrWhiteSpace(spot.StableId))
            {
                Debug.LogError("BuildSpot requires a stable non-empty ID.", spot);
                return;
            }

            if (spots.ContainsKey(spot.StableId))
            {
                Debug.LogError($"Duplicate BuildSpot ID '{spot.StableId}'.", spot);
                return;
            }

            spots.Add(spot.StableId, spot);
            spot.Initialize(this, progression);
        }

        public PurchaseResult TryBuild(BuildSpot spot)
        {
            if (spot == null || spot.Definition == null || spot.Definition.Prefab == null)
            {
                return PurchaseResult.InvalidConfiguration;
            }

            if (spot.IsBuilt)
            {
                return PurchaseResult.AlreadyBuilt;
            }

            if (progression.CurrentLevel < spot.Definition.RequiredLevel)
            {
                return PurchaseResult.Locked;
            }

            if (!wallet.TrySpend(spot.Definition.Cost))
            {
                return PurchaseResult.InsufficientFunds;
            }

            CreateBuilding(spot, true);
            return PurchaseResult.Success;
        }

        public void Restore(IEnumerable<BuiltBuildingData> builtBuildings)
        {
            if (builtBuildings == null)
            {
                return;
            }

            foreach (var savedBuilding in builtBuildings)
            {
                if (savedBuilding == null || !spots.TryGetValue(savedBuilding.BuildSpotId, out var spot))
                {
                    Debug.LogWarning($"Save references unknown BuildSpot '{savedBuilding?.BuildSpotId}'.");
                    continue;
                }

                if (spot.Definition == null || spot.Definition.Id != savedBuilding.BuildingDefinitionId)
                {
                    Debug.LogWarning($"BuildSpot '{spot.StableId}' no longer matches saved building '{savedBuilding.BuildingDefinitionId}'.");
                    continue;
                }

                if (!spot.IsBuilt)
                {
                    CreateBuilding(spot, false);
                }
            }

            StateChanged?.Invoke();
        }

        public List<BuiltBuildingData> CaptureBuiltBuildings()
        {
            var result = new List<BuiltBuildingData>();
            foreach (var pair in spots)
            {
                var spot = pair.Value;
                if (spot != null && spot.IsBuilt && spot.Definition != null)
                {
                    result.Add(new BuiltBuildingData(spot.StableId, spot.Definition.Id));
                }
            }

            return result;
        }

        public void Dispose()
        {
            wallet.BalanceChanged -= OnStateChanged;
            progression.LevelChanged -= OnLevelChanged;
        }

        private void CreateBuilding(BuildSpot spot, bool notify)
        {
            var placement = spot.PlacementRoot;
            var building = UnityEngine.Object.Instantiate(
                spot.Definition.Prefab,
                placement.position,
                placement.rotation,
                placement);
            building.name = spot.Definition.DisplayName;
            spot.MarkBuilt(building);

            var shelf = building.GetComponentInChildren<ShelfStation>(true);
            if (shelf != null)
            {
                stations.Register(shelf);
            }

            var checkout = building.GetComponentInChildren<CheckoutStation>(true);
            if (checkout != null)
            {
                stations.Register(checkout);
            }

            if (notify)
            {
                audio.Play(GameSound.Build);
                BuildingBuilt?.Invoke(spot, spot.Definition);
            }

            StateChanged?.Invoke();
        }

        private void OnStateChanged(int _)
        {
            StateChanged?.Invoke();
        }

        private void OnLevelChanged(int _)
        {
            StateChanged?.Invoke();
        }
    }
}
