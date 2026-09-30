using System;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Customers;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Objectives;
using SupermarketTycoon.Progression;

namespace SupermarketTycoon.UI
{
    public sealed class HudController : IDisposable
    {
        private readonly GameHudView view;
        private readonly IWallet wallet;
        private readonly ProgressionService progression;
        private readonly StoreRatingService rating;
        private readonly ObjectiveService objectives;
        private readonly CustomerSpawner customers;
        private readonly AudioService audio;
        private readonly Action pauseRequested;

        public HudController(
            GameHudView view,
            IWallet wallet,
            ProgressionService progression,
            StoreRatingService rating,
            ObjectiveService objectives,
            CustomerSpawner customers,
            AudioService audio,
            Action pauseRequested)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.rating = rating ?? throw new ArgumentNullException(nameof(rating));
            this.objectives = objectives ?? throw new ArgumentNullException(nameof(objectives));
            this.customers = customers ?? throw new ArgumentNullException(nameof(customers));
            this.audio = audio ?? throw new ArgumentNullException(nameof(audio));
            this.pauseRequested = pauseRequested ?? throw new ArgumentNullException(nameof(pauseRequested));

            wallet.BalanceChanged += OnBalanceChanged;
            progression.XpChanged += OnXpChanged;
            progression.LevelChanged += OnLevelChanged;
            rating.Changed += OnRatingChanged;
            objectives.Changed += RefreshObjective;
            objectives.Completed += OnObjectiveCompleted;
            customers.ActiveCountChanged += OnCustomerCountChanged;
            customers.PaymentCompleted += OnPaymentCompleted;
            view.PauseButton.onClick.AddListener(OnPauseClicked);

            view.SetMoney(wallet.Balance);
            view.SetProgression(progression.CurrentLevel, progression.CurrentXp, progression.XpToNextLevel);
            view.SetCustomers(0);
            view.SetRating(rating.CurrentRating);
            RefreshObjective();
        }

        public void Dispose()
        {
            wallet.BalanceChanged -= OnBalanceChanged;
            progression.XpChanged -= OnXpChanged;
            progression.LevelChanged -= OnLevelChanged;
            rating.Changed -= OnRatingChanged;
            objectives.Changed -= RefreshObjective;
            objectives.Completed -= OnObjectiveCompleted;
            customers.ActiveCountChanged -= OnCustomerCountChanged;
            customers.PaymentCompleted -= OnPaymentCompleted;
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

        private void OnLevelChanged(int level)
        {
            audio.Play(GameSound.LevelUp);
            view.SetProgression(progression.CurrentLevel, progression.CurrentXp, progression.XpToNextLevel);
            view.ShowNotification($"LEVEL {level}", $"NEW: {progression.GetUnlockSummary(level)}");
        }

        private void OnRatingChanged(float value)
        {
            view.SetRating(value);
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
            var current = objectives.Current;
            var progress = current != null && current.Type == ObjectiveType.ReachRating
                ? $"{objectives.CurrentProgress / 10f:0.0} / {current.Target / 10f:0.0}"
                : current != null ? $"{objectives.CurrentProgress} / {current.Target}" : string.Empty;
            view.SetObjective(
                current != null ? current.DisplayName : "Store Established",
                progress,
                objectives.IsSequenceComplete);
        }

        private void OnObjectiveCompleted(ObjectiveDefinition objective)
        {
            var reward = objective.MoneyReward > 0 || objective.XpReward > 0
                ? $"REWARD  ${objective.MoneyReward}  {objective.XpReward} XP"
                : string.Empty;
            view.ShowNotification("OBJECTIVE COMPLETE", reward);
        }

        private void OnPauseClicked()
        {
            audio.Play(GameSound.UiClick);
            pauseRequested();
        }
    }
}
