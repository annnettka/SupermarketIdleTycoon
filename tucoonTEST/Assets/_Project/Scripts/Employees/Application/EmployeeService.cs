using System;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Progression;
using UnityEngine;

namespace SupermarketTycoon.Employees
{
    public enum EmployeePurchaseResult
    {
        Success,
        MaxLevel,
        Locked,
        InsufficientFunds,
        InvalidConfiguration
    }

    public sealed class EmployeeService
    {
        private readonly EmployeeDefinition definition;
        private readonly IWallet wallet;
        private readonly ProgressionService progression;
        private readonly StationRegistry stations;
        private readonly AudioService audio;
        private readonly GameObject employeeVisual;

        public EmployeeService(
            EmployeeDefinition definition,
            IWallet wallet,
            ProgressionService progression,
            StationRegistry stations,
            AudioService audio,
            GameObject employeeVisual)
        {
            this.definition = definition;
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.stations = stations ?? throw new ArgumentNullException(nameof(stations));
            this.audio = audio ?? throw new ArgumentNullException(nameof(audio));
            this.employeeVisual = employeeVisual;
        }

        public EmployeeDefinition Definition => definition;
        public int CurrentLevel { get; private set; }
        public float SpeedMultiplier => CurrentLevel > 0
            ? 1f + definition.GetLevel(CurrentLevel).CheckoutSpeedBonus
            : 1f;

        public event Action Changed;

        public void Restore(int level)
        {
            CurrentLevel = definition == null ? 0 : Mathf.Clamp(level, 0, definition.MaxLevel);
            ApplyState();
        }

        public EmployeePurchaseResult CanPurchaseNext()
        {
            if (definition == null || definition.MaxLevel <= 0)
            {
                return EmployeePurchaseResult.InvalidConfiguration;
            }

            if (CurrentLevel >= definition.MaxLevel)
            {
                return EmployeePurchaseResult.MaxLevel;
            }

            var next = definition.GetLevel(CurrentLevel + 1);
            if (progression.CurrentLevel < next.RequiredPlayerLevel)
            {
                return EmployeePurchaseResult.Locked;
            }

            return wallet.CanSpend(next.Cost)
                ? EmployeePurchaseResult.Success
                : EmployeePurchaseResult.InsufficientFunds;
        }

        public EmployeePurchaseResult TryPurchaseNext()
        {
            var result = CanPurchaseNext();
            if (result != EmployeePurchaseResult.Success)
            {
                return result;
            }

            var next = definition.GetLevel(CurrentLevel + 1);
            if (!wallet.TrySpend(next.Cost))
            {
                return EmployeePurchaseResult.InsufficientFunds;
            }

            CurrentLevel++;
            ApplyState();
            audio.Play(GameSound.Build);
            Changed?.Invoke();
            return EmployeePurchaseResult.Success;
        }

        private void ApplyState()
        {
            stations.SetCheckoutEmployeeSpeedMultiplier(SpeedMultiplier);
            if (employeeVisual != null)
            {
                employeeVisual.SetActive(CurrentLevel > 0);
            }
        }
    }
}
