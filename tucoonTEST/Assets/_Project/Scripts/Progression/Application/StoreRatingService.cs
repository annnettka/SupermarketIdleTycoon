using System;
using UnityEngine;

namespace SupermarketTycoon.Progression
{
    /// <summary>
    /// Maintains bounded store rating and derives customer traffic pacing from service outcomes.
    /// Поддерживает рейтинг магазина в допустимых границах и определяет темп покупателей по результатам обслуживания.
    /// </summary>
    public sealed class StoreRatingService
    {
        public StoreRatingService(float loadedRating)
        {
            CurrentRating = Mathf.Clamp(loadedRating <= 0f ? 3f : loadedRating, 1f, 5f);
        }

        public float CurrentRating { get; private set; }
        public float TrafficIntervalMultiplier => CurrentRating < 3f
            ? 1.15f
            : CurrentRating >= 4f ? 0.9f : 1f;

        public event Action<float> Changed;

        public void RecordSuccessfulTransaction()
        {
            Change(0.03f);
        }

        public void RecordCustomerLost()
        {
            Change(-0.12f);
        }

        public void RecordStoreUpgrade()
        {
            Change(0.05f);
        }

        private void Change(float delta)
        {
            var next = Mathf.Clamp(CurrentRating + delta, 1f, 5f);
            if (Mathf.Approximately(next, CurrentRating))
            {
                return;
            }

            CurrentRating = next;
            Changed?.Invoke(CurrentRating);
        }
    }
}
