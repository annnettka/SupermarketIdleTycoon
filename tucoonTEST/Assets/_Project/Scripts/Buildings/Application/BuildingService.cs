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
        private readonly Dictionary<string, BuiltBuildingData> builtStates =
            new Dictionary<string, BuiltBuildingData>();

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
        public event Action<BuildSpot, int> BuildingLevelChanged;
        public event Action<BuildSpot> BuildingSelected;

        public int BuiltCount
        {
            get
            {
                var count = 0;
                foreach (var pair in spots)
                {
                    if (pair.Value != null && pair.Value.IsBuilt)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int CountBuilt(BuildingType type)
        {
            var count = 0;
            foreach (var pair in spots)
            {
                var spot = pair.Value;
                if (spot != null && spot.IsBuilt && spot.Definition != null && spot.Definition.Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        public int GetHighestLevel(BuildingType type)
        {
            var highest = 0;
            foreach (var pair in spots)
            {
                var spot = pair.Value;
                if (spot != null && spot.IsBuilt && spot.Definition != null && spot.Definition.Type == type)
                {
                    highest = Math.Max(highest, spot.CurrentLevel);
                }
            }

            return highest;
        }

        public bool IsDefinitionBuilt(string definitionId)
        {
            foreach (var pair in spots)
            {
                var spot = pair.Value;
                if (spot != null && spot.IsBuilt && spot.Definition != null &&
                    string.Equals(spot.Definition.Id, definitionId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetHighestLevel(string definitionId)
        {
            var highest = 0;
            foreach (var pair in spots)
            {
                var spot = pair.Value;
                if (spot != null && spot.IsBuilt && spot.Definition != null &&
                    string.Equals(spot.Definition.Id, definitionId, StringComparison.Ordinal))
                {
                    highest = Math.Max(highest, spot.CurrentLevel);
                }
            }

            return highest;
        }

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

            if (!spot.IsExpansionUnlocked || progression.CurrentLevel < spot.Definition.RequiredLevel)
            {
                return PurchaseResult.Locked;
            }

            if (!wallet.TrySpend(spot.Definition.Cost))
            {
                return PurchaseResult.InsufficientFunds;
            }

            CreateBuilding(spot, 1, true);
            return PurchaseResult.Success;
        }

        public void Select(BuildSpot spot)
        {
            if (spot != null && spot.IsBuilt)
            {
                BuildingSelected?.Invoke(spot);
            }
        }

        public bool ApplyLevel(BuildSpot spot, int level)
        {
            if (spot == null || !spot.IsBuilt || spot.Definition == null ||
                level < 1 || level > spot.Definition.MaxLevel)
            {
                return false;
            }

            spot.SetLevel(level);
            ApplyBuildingLevel(spot);
            builtStates[spot.StableId] = new BuiltBuildingData(
                spot.StableId,
                spot.Definition.Id,
                spot.CurrentLevel);
            BuildingLevelChanged?.Invoke(spot, level);
            StateChanged?.Invoke();
            return true;
        }

        public void SetExpansionUnlocked(string expansionId, bool unlocked)
        {
            foreach (var pair in spots)
            {
                var spot = pair.Value;
                if (spot != null && string.Equals(
                        spot.RequiredExpansionId,
                        expansionId,
                        StringComparison.Ordinal))
                {
                    spot.SetExpansionUnlocked(unlocked);
                }
            }

            StateChanged?.Invoke();
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
                    CreateBuilding(spot, Mathf.Max(1, savedBuilding.BuildingLevel), false);
                }
            }

            StateChanged?.Invoke();
        }

        public List<BuiltBuildingData> CaptureBuiltBuildings()
        {
            var result = new List<BuiltBuildingData>();
            foreach (var pair in builtStates)
            {
                var saved = pair.Value;
                result.Add(new BuiltBuildingData(
                    saved.BuildSpotId,
                    saved.BuildingDefinitionId,
                    saved.BuildingLevel));
            }

            return result;
        }

        public void GetOfflineMetrics(
            out int shelfCapacity,
            out int checkoutCapacity,
            out float averageIncomeMultiplier)
        {
            shelfCapacity = 0;
            checkoutCapacity = 0;
            var multiplierTotal = 0f;
            var shelfCount = 0;

            foreach (var pair in spots)
            {
                var spot = pair.Value;
                if (spot == null || !spot.IsBuilt || spot.Definition == null)
                {
                    continue;
                }

                var level = spot.Definition.GetLevel(spot.CurrentLevel);
                if (spot.Definition.Type == BuildingType.Shelf)
                {
                    shelfCapacity += level.Capacity;
                    multiplierTotal += level.IncomeMultiplier;
                    shelfCount++;
                }
                else if (spot.Definition.Type == BuildingType.Checkout)
                {
                    checkoutCapacity += level.Capacity;
                }
            }

            averageIncomeMultiplier = shelfCount > 0 ? multiplierTotal / shelfCount : 1f;
        }

        public void Dispose()
        {
            wallet.BalanceChanged -= OnStateChanged;
            progression.LevelChanged -= OnLevelChanged;
        }

        private void CreateBuilding(BuildSpot spot, int level, bool notify)
        {
            var placement = spot.PlacementRoot;
            var building = UnityEngine.Object.Instantiate(
                spot.Definition.Prefab,
                placement.position,
                placement.rotation,
                placement);
            building.name = spot.Definition.DisplayName;
            spot.MarkBuilt(building, level);
            builtStates[spot.StableId] = new BuiltBuildingData(
                spot.StableId,
                spot.Definition.Id,
                spot.CurrentLevel);

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

            ApplyBuildingLevel(spot);

            if (notify)
            {
                audio.Play(GameSound.Build);
                BuildingBuilt?.Invoke(spot, spot.Definition);
            }

            StateChanged?.Invoke();
        }

        private static void ApplyBuildingLevel(BuildSpot spot)
        {
            var level = spot.Definition.GetLevel(spot.CurrentLevel);
            var shelf = spot.BuildingObject.GetComponentInChildren<ShelfStation>(true);
            shelf?.ApplyLevel(level);

            var checkout = spot.BuildingObject.GetComponentInChildren<CheckoutStation>(true);
            checkout?.ApplyLevel(level);

            spot.BuildingObject.transform.localScale = Vector3.one * level.VisualScale;
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
