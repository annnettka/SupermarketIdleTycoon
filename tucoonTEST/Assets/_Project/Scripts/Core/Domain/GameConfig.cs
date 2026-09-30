using UnityEngine;

namespace SupermarketTycoon.Core
{
    [CreateAssetMenu(menuName = "Supermarket Tycoon/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private int startingMoney = 150;
        [SerializeField, Min(1)] private int customerPayment = 15;
        [SerializeField, Min(1)] private int customerXpReward = 10;
        [SerializeField, Min(0.1f)] private float spawnInterval = 2.75f;
        [SerializeField, Range(1, 10)] private int maximumActiveCustomers = 10;
        [SerializeField, Min(0f)] private float minimumLoadingDuration = 0.3f;

        public int StartingMoney => startingMoney;
        public int CustomerPayment => customerPayment;
        public int CustomerXpReward => customerXpReward;
        public float SpawnInterval => spawnInterval;
        public int MaximumActiveCustomers => maximumActiveCustomers;
        public float MinimumLoadingDuration => minimumLoadingDuration;
    }
}
