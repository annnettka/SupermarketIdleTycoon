using System;

namespace SupermarketTycoon.Progression
{
    public sealed class ProgressionService
    {
        private readonly ProgressionConfig config;

        public ProgressionService(ProgressionConfig config, int currentLevel, int currentXp)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            CurrentLevel = Math.Max(1, currentLevel);
            CurrentXp = Math.Max(0, currentXp);
            NormalizeLoadedProgress();
        }

        public int CurrentLevel { get; private set; }
        public int CurrentXp { get; private set; }
        public int XpToNextLevel => config.GetRequiredXp(CurrentLevel);

        public event Action<int> LevelChanged;
        public event Action<int, int> XpChanged;

        public void AddXp(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            CurrentXp += amount;
            while (CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                CurrentLevel++;
                LevelChanged?.Invoke(CurrentLevel);
            }

            XpChanged?.Invoke(CurrentXp, XpToNextLevel);
        }

        private void NormalizeLoadedProgress()
        {
            while (CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                CurrentLevel++;
            }
        }
    }
}
