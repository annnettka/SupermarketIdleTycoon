using System;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Core;
using SupermarketTycoon.Economy;
using SupermarketTycoon.Progression;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace SupermarketTycoon.Customers
{
    public sealed class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] private CustomerAgent customerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform exitPoint;

        private ObjectPool<CustomerAgent> pool;
        private StationRegistry stations;
        private EconomyService economy;
        private ProgressionService progression;
        private StoreRatingService rating;
        private PauseService pause;
        private CustomerConfig config;
        private CustomerProfileDefinition[] profiles;
        private int xpReward;
        private int fallbackMaximumActive;
        private float spawnInterval;
        private float spawnTimer;
        private int activeCount;
        private bool initialized;

        public event Action<int> ActiveCountChanged;
        public event Action<Vector3, int> PaymentCompleted;
        public event Action<Vector3> CustomerLost;

        public void Configure(CustomerAgent prefab, Transform spawn, Transform exit)
        {
            customerPrefab = prefab;
            spawnPoint = spawn;
            exitPoint = exit;
        }

        public void Initialize(
            StationRegistry stationRegistry,
            EconomyService economyService,
            ProgressionService progressionService,
            StoreRatingService ratingService,
            PauseService pauseService,
            CustomerConfig customerConfig,
            CustomerProfileDefinition[] customerProfiles,
            int rewardXp,
            float interval,
            int maxActive)
        {
            stations = stationRegistry;
            economy = economyService;
            progression = progressionService;
            rating = ratingService;
            pause = pauseService;
            config = customerConfig;
            profiles = customerProfiles;
            xpReward = rewardXp;
            spawnInterval = Mathf.Max(0.1f, interval);
            fallbackMaximumActive = Mathf.Max(1, maxActive);
            spawnTimer = 0.25f;

            var poolSize = Mathf.Max(fallbackMaximumActive, config.MaxConfiguredActiveCustomers);
            pool = new ObjectPool<CustomerAgent>(
                CreateCustomer,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPooledCustomer,
                true,
                poolSize,
                poolSize);
            initialized = true;
        }

        public void Shutdown()
        {
            initialized = false;
            pool?.Clear();
            pool = null;
            activeCount = 0;
        }

        private void Update()
        {
            var maximumActive = config != null
                ? config.GetMaximumActiveCustomers(progression.CurrentLevel, fallbackMaximumActive)
                : fallbackMaximumActive;
            if (!initialized || pause.IsPaused || !stations.IsOperational || activeCount >= maximumActive)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            spawnTimer = spawnInterval * (rating != null ? rating.TrafficIntervalMultiplier : 1f);
            pool.Get();
        }

        private CustomerAgent CreateCustomer()
        {
            var customer = Instantiate(customerPrefab, transform);
            customer.gameObject.SetActive(false);
            return customer;
        }

        private void OnTakeFromPool(CustomerAgent customer)
        {
            customer.transform.SetPositionAndRotation(GetSpawnPosition(), spawnPoint.rotation);
            customer.gameObject.SetActive(true);
            activeCount++;
            ActiveCountChanged?.Invoke(activeCount);

            var runtimeContext = new CustomerRuntimeContext(
                config,
                ChooseProfile(),
                stations,
                economy,
                progression,
                pause,
                exitPoint,
                xpReward,
                ReleaseCustomer,
                OnPaymentCompleted,
                OnCustomerLost);
            customer.Begin(runtimeContext);
        }

        private void OnReturnedToPool(CustomerAgent customer)
        {
            customer.PrepareForPool();
            customer.gameObject.SetActive(false);
            activeCount = Mathf.Max(0, activeCount - 1);
            ActiveCountChanged?.Invoke(activeCount);
        }

        private static void OnDestroyPooledCustomer(CustomerAgent customer)
        {
            if (customer != null)
            {
                Destroy(customer.gameObject);
            }
        }

        private void ReleaseCustomer(CustomerAgent customer)
        {
            if (initialized && customer != null && customer.gameObject.activeSelf)
            {
                pool.Release(customer);
            }
        }

        private void OnPaymentCompleted(Vector3 position, int amount)
        {
            PaymentCompleted?.Invoke(position, amount);
        }

        private void OnCustomerLost(Vector3 position)
        {
            CustomerLost?.Invoke(position);
        }

        private CustomerProfileDefinition ChooseProfile()
        {
            if (profiles == null || profiles.Length == 0)
            {
                return null;
            }

            var totalWeight = 0f;
            for (var i = 0; i < profiles.Length; i++)
            {
                var profile = profiles[i];
                if (profile != null && progression.CurrentLevel >= profile.RequiredLevel)
                {
                    totalWeight += profile.SpawnWeight;
                }
            }

            if (totalWeight <= 0f)
            {
                return profiles[0];
            }

            var roll = UnityEngine.Random.value * totalWeight;
            for (var i = 0; i < profiles.Length; i++)
            {
                var profile = profiles[i];
                if (profile == null || progression.CurrentLevel < profile.RequiredLevel)
                {
                    continue;
                }

                roll -= profile.SpawnWeight;
                if (roll <= 0f)
                {
                    return profile;
                }
            }

            return profiles[0];
        }

        private Vector3 GetSpawnPosition()
        {
            if (NavMesh.SamplePosition(spawnPoint.position, out var hit, 4f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return spawnPoint.position;
        }
    }
}
