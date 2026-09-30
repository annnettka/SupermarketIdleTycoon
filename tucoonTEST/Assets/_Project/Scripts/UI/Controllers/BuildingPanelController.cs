using System;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Progression;
using UnityEngine;

namespace SupermarketTycoon.UI
{
    public sealed class BuildingPanelController : IDisposable
    {
        private readonly BuildingPanelView view;
        private readonly BuildingService buildings;
        private readonly BuildingUpgradeService upgrades;
        private readonly IWallet wallet;
        private readonly ProgressionService progression;
        private readonly int baseCustomerPayment;
        private BuildSpot selected;

        public BuildingPanelController(
            BuildingPanelView view,
            BuildingService buildings,
            BuildingUpgradeService upgrades,
            IWallet wallet,
            ProgressionService progression,
            int baseCustomerPayment)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            this.upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.baseCustomerPayment = Math.Max(1, baseCustomerPayment);

            buildings.BuildingSelected += Select;
            buildings.BuildingLevelChanged += OnBuildingLevelChanged;
            wallet.BalanceChanged += OnBalanceChanged;
            progression.LevelChanged += OnPlayerLevelChanged;
            view.UpgradeButton.onClick.AddListener(OnUpgradeClicked);
            view.CloseButton.onClick.AddListener(Close);
            view.SetVisible(false);
        }

        public void Dispose()
        {
            buildings.BuildingSelected -= Select;
            buildings.BuildingLevelChanged -= OnBuildingLevelChanged;
            wallet.BalanceChanged -= OnBalanceChanged;
            progression.LevelChanged -= OnPlayerLevelChanged;
            view.UpgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            view.CloseButton.onClick.RemoveListener(Close);
        }

        private void Select(BuildSpot spot)
        {
            selected = spot;
            view.SetVisible(true);
            Refresh();
        }

        private void Close()
        {
            selected = null;
            view.SetVisible(false);
        }

        private void OnUpgradeClicked()
        {
            if (selected != null)
            {
                upgrades.TryUpgrade(selected);
                Refresh();
            }
        }

        private void OnBuildingLevelChanged(BuildSpot spot, int _)
        {
            if (spot == selected)
            {
                Refresh();
            }
        }

        private void OnBalanceChanged(int _)
        {
            Refresh();
        }

        private void OnPlayerLevelChanged(int _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (selected == null || selected.Definition == null)
            {
                return;
            }

            var definition = selected.Definition;
            var current = definition.GetLevel(selected.CurrentLevel);
            var result = upgrades.CanUpgrade(selected);
            var hasNext = definition.TryGetNextLevel(selected.CurrentLevel, out var next);
            var primary = definition.Type == BuildingType.Shelf
                ? FormatIncome(current, hasNext, next)
                : FormatSpeed(current, hasNext, next);
            var secondary = $"CAPACITY  {current.Capacity}" + (hasNext ? $"  >  {next.Capacity}" : string.Empty);
            var button = GetButtonText(result, hasNext ? next : default);
            view.Show(definition.DisplayName, selected.CurrentLevel, primary, secondary, button, result == UpgradeResult.Success);
        }

        private string FormatIncome(BuildingLevelDefinition current, bool hasNext, BuildingLevelDefinition next)
        {
            var currentIncome = Mathf.RoundToInt(baseCustomerPayment * current.IncomeMultiplier);
            return hasNext
                ? $"INCOME  +${currentIncome}  >  +${Mathf.RoundToInt(baseCustomerPayment * next.IncomeMultiplier)}"
                : $"INCOME  +${currentIncome}";
        }

        private static string FormatSpeed(BuildingLevelDefinition current, bool hasNext, BuildingLevelDefinition next)
        {
            return hasNext
                ? $"SERVICE  {current.InteractionDuration:0.0}s  >  {next.InteractionDuration:0.0}s"
                : $"SERVICE  {current.InteractionDuration:0.0}s";
        }

        private static string GetButtonText(UpgradeResult result, BuildingLevelDefinition next)
        {
            return result switch
            {
                UpgradeResult.MaxLevel => "MAX LEVEL",
                UpgradeResult.Locked => $"REQUIRES LEVEL {next.RequiredPlayerLevel}",
                UpgradeResult.InsufficientFunds => $"UPGRADE  ${next.UpgradeCost}",
                UpgradeResult.Success => $"UPGRADE  ${next.UpgradeCost}",
                _ => "UNAVAILABLE"
            };
        }
    }
}
