using System;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Employees;
using SupermarketTycoon.Expansion;
using SupermarketTycoon.Progression;

namespace SupermarketTycoon.Objectives
{
    public sealed class ObjectiveService
    {
        private readonly ObjectiveConfig config;
        private readonly EconomyService economy;
        private readonly ProgressionService progression;
        private bool isCompleting;

        public ObjectiveService(
            ObjectiveConfig config,
            EconomyService economy,
            ProgressionService progression,
            int currentIndex,
            int currentProgress)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            CurrentIndex = Math.Max(0, currentIndex);
            CurrentProgress = Math.Max(0, currentProgress);
            RefreshAbsoluteProgress(false);
        }

        public int CurrentIndex { get; private set; }
        public int CurrentProgress { get; private set; }
        public ObjectiveDefinition Current => config.Get(CurrentIndex);
        public bool IsSequenceComplete => Current == null;

        public event Action Changed;
        public event Action<ObjectiveDefinition> Completed;

        public void RecordBuildingBuilt(BuildingDefinition definition)
        {
            RecordMatching(ObjectiveType.BuildBuilding, definition != null ? definition.Id : null, 1);
        }

        public void RecordBuildingUpgraded(BuildingDefinition definition)
        {
            RecordMatching(ObjectiveType.UpgradeBuilding, definition != null ? definition.Id : null, 1);
        }

        public void RecordCustomerServed()
        {
            RecordMatching(ObjectiveType.ServeCustomers, null, 1);
        }

        public void RecordMoneyEarned(int amount)
        {
            if (!isCompleting && amount > 0)
            {
                RecordMatching(ObjectiveType.EarnMoney, null, amount);
            }
        }

        public void RecordEmployeePurchased(string employeeId)
        {
            RecordMatching(ObjectiveType.BuyEmployee, employeeId, 1);
        }

        public void RecordExpansionPurchased(string expansionId)
        {
            RecordMatching(ObjectiveType.BuyExpansion, expansionId, 1);
        }

        public void RefreshLevel()
        {
            RefreshAbsoluteProgress(true);
        }

        public void RefreshRating(float rating)
        {
            if (Current != null && Current.Type == ObjectiveType.ReachRating)
            {
                SetProgress((int)Math.Round(rating * 10f));
            }
        }

        public void SynchronizeExistingState(
            BuildingService buildings,
            EmployeeService employees,
            StoreExpansionService expansions,
            StoreRatingService rating)
        {
            var remainingChecks = config.Count + 1;
            while (Current != null && remainingChecks-- > 0)
            {
                var before = CurrentIndex;
                var current = Current;
                switch (current.Type)
                {
                    case ObjectiveType.BuildBuilding:
                        if (buildings != null && buildings.IsDefinitionBuilt(current.FilterId))
                        {
                            SetProgress(current.Target);
                        }

                        break;
                    case ObjectiveType.UpgradeBuilding:
                        if (buildings != null && buildings.GetHighestLevel(current.FilterId) >= 2)
                        {
                            SetProgress(current.Target);
                        }

                        break;
                    case ObjectiveType.ReachLevel:
                        SetProgress(progression.CurrentLevel);
                        break;
                    case ObjectiveType.BuyEmployee:
                        if (employees != null && employees.CurrentLevel > 0)
                        {
                            SetProgress(current.Target);
                        }

                        break;
                    case ObjectiveType.BuyExpansion:
                        if (expansions != null && expansions.IsPurchased(current.FilterId))
                        {
                            SetProgress(current.Target);
                        }

                        break;
                    case ObjectiveType.ReachRating:
                        if (rating != null)
                        {
                            SetProgress((int)Math.Round(rating.CurrentRating * 10f));
                        }

                        break;
                }

                if (before == CurrentIndex)
                {
                    break;
                }
            }

            Changed?.Invoke();
        }

        private void RecordMatching(ObjectiveType type, string id, int amount)
        {
            var current = Current;
            if (current == null || current.Type != type || amount <= 0)
            {
                return;
            }

            if (!string.IsNullOrEmpty(current.FilterId) &&
                !string.Equals(current.FilterId, id, StringComparison.Ordinal))
            {
                return;
            }

            SetProgress(CurrentProgress + amount);
        }

        private void RefreshAbsoluteProgress(bool notify)
        {
            var current = Current;
            if (current == null)
            {
                if (notify)
                {
                    Changed?.Invoke();
                }

                return;
            }

            if (current.Type == ObjectiveType.ReachLevel)
            {
                SetProgress(progression.CurrentLevel);
            }
            else if (notify)
            {
                Changed?.Invoke();
            }
        }

        private void SetProgress(int value)
        {
            var current = Current;
            if (current == null)
            {
                return;
            }

            CurrentProgress = Math.Max(0, Math.Min(value, current.Target));
            Changed?.Invoke();
            if (CurrentProgress >= current.Target)
            {
                CompleteCurrent(current);
            }
        }

        private void CompleteCurrent(ObjectiveDefinition completed)
        {
            if (isCompleting)
            {
                return;
            }

            isCompleting = true;
            if (completed.MoneyReward > 0)
            {
                economy.AddIncome(completed.MoneyReward);
            }

            if (completed.XpReward > 0 && !progression.IsMaxLevel)
            {
                progression.AddXp(completed.XpReward);
            }

            Completed?.Invoke(completed);
            CurrentIndex++;
            CurrentProgress = 0;
            isCompleting = false;
            RefreshAbsoluteProgress(true);
        }
    }
}
