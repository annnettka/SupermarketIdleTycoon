using System;

namespace SupermarketTycoon.Progression
{
    public sealed class ProgressionService
    {
        private readonly ProgressionConfig config;

        public ProgressionService(ProgressionConfig config, int currentLevel, int currentXp)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            CurrentLevel = Math.Min(config.MaxLevel, Math.Max(1, currentLevel));
            CurrentXp = Math.Max(0, currentXp);
            NormalizeLoadedProgress();
        }

        public int CurrentLevel { get; private set; }
        public int CurrentXp { get; private set; }
        public int MaxLevel => config.MaxLevel;
        public int XpToNextLevel => config.GetRequiredXp(CurrentLevel);
        public bool IsMaxLevel => CurrentLevel >= MaxLevel;

        public event Action<int> LevelChanged;
        public event Action<int, int> XpChanged;

        public string GetUnlockSummary(int level)
        {
            return config.GetUnlockSummary(level);
        }

        public void AddXp(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (IsMaxLevel)
            {
                return;
            }

            CurrentXp += amount;
            while (!IsMaxLevel && CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                CurrentLevel++;
                LevelChanged?.Invoke(CurrentLevel);
            }

            if (IsMaxLevel)
            {
                CurrentXp = 0;
            }

            XpChanged?.Invoke(CurrentXp, XpToNextLevel);
        }

        private void NormalizeLoadedProgress()
        {
            while (!IsMaxLevel && CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                CurrentLevel++;
            }

            if (IsMaxLevel)
            {
                CurrentXp = 0;
            }
        }
    }
}
