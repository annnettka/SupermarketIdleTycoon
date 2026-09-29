using System;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Core;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Progression;
using UnityEngine;

namespace SupermarketTycoon.Customers
{
    public sealed class CustomerRuntimeContext
    {
        public CustomerRuntimeContext(
            CustomerConfig config,
            StationRegistry stations,
            EconomyService economy,
            ProgressionService progression,
            PauseService pause,
            Transform exitPoint,
            int xpReward,
            Action<CustomerAgent> release,
            Action<Vector3, int> paymentCompleted)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Stations = stations ?? throw new ArgumentNullException(nameof(stations));
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            Progression = progression ?? throw new ArgumentNullException(nameof(progression));
            Pause = pause ?? throw new ArgumentNullException(nameof(pause));
            ExitPoint = exitPoint != null ? exitPoint : throw new ArgumentNullException(nameof(exitPoint));
            XpReward = Math.Max(1, xpReward);
            Release = release ?? throw new ArgumentNullException(nameof(release));
            PaymentCompleted = paymentCompleted;
        }

        public CustomerConfig Config { get; }
        public StationRegistry Stations { get; }
        public EconomyService Economy { get; }
        public ProgressionService Progression { get; }
        public PauseService Pause { get; }
        public Transform ExitPoint { get; }
        public int XpReward { get; }
        public Action<CustomerAgent> Release { get; }
        public Action<Vector3, int> PaymentCompleted { get; }
    }
}
