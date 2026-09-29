using System;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Customers;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Progression;

namespace SupermarketTycoon.UI
{
    public sealed class HudController : IDisposable
    {
        private readonly GameHudView view;
        private readonly IWallet wallet;
        private readonly ProgressionService progression;
        private readonly CustomerSpawner customers;
        private readonly BuildingService buildings;
        private readonly StationRegistry stations;
        private readonly AudioService audio;
        private readonly Action pauseRequested;

        public HudController(
            GameHudView view,
            IWallet wallet,
            ProgressionService progression,
            CustomerSpawner customers,
            BuildingService buildings,
            StationRegistry stations,
            AudioService audio,
            Action pauseRequested)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.customers = customers ?? throw new ArgumentNullException(nameof(customers));
            this.buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            this.stations = stations ?? throw new ArgumentNullException(nameof(stations));
            this.audio = audio ?? throw new ArgumentNullException(nameof(audio));
            this.pauseRequested = pauseRequested ?? throw new ArgumentNullException(nameof(pauseRequested));

            wallet.BalanceChanged += OnBalanceChanged;
            progression.XpChanged += OnXpChanged;
            progression.LevelChanged += OnLevelChanged;
            customers.ActiveCountChanged += OnCustomerCountChanged;
            customers.PaymentCompleted += OnPaymentCompleted;
            buildings.StateChanged += RefreshObjective;
            view.PauseButton.onClick.AddListener(OnPauseClicked);

            view.SetMoney(wallet.Balance);
            view.SetProgression(progression.CurrentLevel, progression.CurrentXp, progression.XpToNextLevel);
            view.SetCustomers(0);
            RefreshObjective();
        }

        public void Dispose()
        {
            wallet.BalanceChanged -= OnBalanceChanged;
            progression.XpChanged -= OnXpChanged;
            progression.LevelChanged -= OnLevelChanged;
            customers.ActiveCountChanged -= OnCustomerCountChanged;
            customers.PaymentCompleted -= OnPaymentCompleted;
            buildings.StateChanged -= RefreshObjective;
            view.PauseButton.onClick.RemoveListener(OnPauseClicked);
        }

        private void OnBalanceChanged(int balance)
        {
            view.SetMoney(balance);
        }

        private void OnXpChanged(int xp, int required)
        {
            view.SetProgression(progression.CurrentLevel, xp, required);
        }

        private void OnLevelChanged(int _)
        {
            audio.Play(GameSound.LevelUp);
            view.SetProgression(progression.CurrentLevel, progression.CurrentXp, progression.XpToNextLevel);
        }

        private void OnCustomerCountChanged(int count)
        {
            view.SetCustomers(count);
        }

        private void OnPaymentCompleted(UnityEngine.Vector3 _, int amount)
        {
            audio.Play(GameSound.Income);
            view.ShowIncome(amount);
        }

        private void RefreshObjective()
        {
            view.SetObjective(stations.IsOperational);
        }

        private void OnPauseClicked()
        {
            audio.Play(GameSound.UiClick);
            pauseRequested();
        }
    }
}
