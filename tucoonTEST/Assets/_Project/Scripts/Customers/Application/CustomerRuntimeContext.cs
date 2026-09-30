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
            CustomerProfileDefinition profile,
            StationRegistry stations,
            EconomyService economy,
            ProgressionService progression,
            PauseService pause,
            Transform exitPoint,
            int xpReward,
            Action<CustomerAgent> release,
            Action<Vector3, int> paymentCompleted,
            Action<Vector3> customerLost)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Profile = profile;
            Stations = stations ?? throw new ArgumentNullException(nameof(stations));
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            Progression = progression ?? throw new ArgumentNullException(nameof(progression));
            Pause = pause ?? throw new ArgumentNullException(nameof(pause));
            ExitPoint = exitPoint != null ? exitPoint : throw new ArgumentNullException(nameof(exitPoint));
            XpReward = Math.Max(1, xpReward);
            Release = release ?? throw new ArgumentNullException(nameof(release));
            PaymentCompleted = paymentCompleted;
            CustomerLost = customerLost;
        }

        public CustomerConfig Config { get; }
        public CustomerProfileDefinition Profile { get; }
        public StationRegistry Stations { get; }
        public EconomyService Economy { get; }
        public ProgressionService Progression { get; }
        public PauseService Pause { get; }
        public Transform ExitPoint { get; }
        public int XpReward { get; }
        public Action<CustomerAgent> Release { get; }
        public Action<Vector3, int> PaymentCompleted { get; }
        public Action<Vector3> CustomerLost { get; }

        public float MovementSpeedMultiplier => Profile != null ? Profile.MovementSpeedMultiplier : 1f;
        public float ShoppingTimeMultiplier => Profile != null ? Profile.ShoppingTimeMultiplier : 1f;
        public float ProfilePaymentMultiplier => Profile != null ? Profile.PaymentMultiplier : 1f;
        public float QueuePatience => Profile != null ? Profile.QueuePatience : 15f;
    }
}
