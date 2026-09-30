using System;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Progression;

namespace SupermarketTycoon.Buildings
{
    public enum UpgradeResult
    {
        Success,
        NotBuilt,
        MaxLevel,
        Locked,
        InsufficientFunds,
        InvalidConfiguration
    }

    public sealed class BuildingUpgradeService
    {
        private readonly IWallet wallet;
        private readonly ProgressionService progression;
        private readonly BuildingService buildings;
        private readonly AudioService audio;

        public BuildingUpgradeService(
            IWallet wallet,
            ProgressionService progression,
            BuildingService buildings,
            AudioService audio)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            this.audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        public event Action<BuildSpot, int> Upgraded;

        public UpgradeResult CanUpgrade(BuildSpot spot)
        {
            if (spot == null || spot.Definition == null)
            {
                return UpgradeResult.InvalidConfiguration;
            }

            if (!spot.IsBuilt)
            {
                return UpgradeResult.NotBuilt;
            }

            if (!spot.Definition.TryGetNextLevel(spot.CurrentLevel, out var next))
            {
                return UpgradeResult.MaxLevel;
            }

            if (progression.CurrentLevel < next.RequiredPlayerLevel)
            {
                return UpgradeResult.Locked;
            }

            return wallet.CanSpend(next.UpgradeCost)
                ? UpgradeResult.Success
                : UpgradeResult.InsufficientFunds;
        }

        public UpgradeResult TryUpgrade(BuildSpot spot)
        {
            var result = CanUpgrade(spot);
            if (result != UpgradeResult.Success)
            {
                return result;
            }

            var next = spot.Definition.GetLevel(spot.CurrentLevel + 1);
            if (!wallet.TrySpend(next.UpgradeCost))
            {
                return UpgradeResult.InsufficientFunds;
            }

            if (!buildings.ApplyLevel(spot, spot.CurrentLevel + 1))
            {
                wallet.Add(next.UpgradeCost);
                return UpgradeResult.InvalidConfiguration;
            }

            audio.Play(GameSound.Build);
            spot.BuildingObject.GetComponent<BuildingUpgradeFeedback>()?.Play();
            Upgraded?.Invoke(spot, spot.CurrentLevel);
            return UpgradeResult.Success;
        }
    }
}
