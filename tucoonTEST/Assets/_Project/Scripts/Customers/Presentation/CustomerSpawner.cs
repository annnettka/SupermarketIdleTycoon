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
        private PauseService pause;
        private CustomerConfig config;
        private int xpReward;
        private int maximumActive;
        private float spawnInterval;
        private float spawnTimer;
        private int activeCount;
        private bool initialized;

        public event Action<int> ActiveCountChanged;
        public event Action<Vector3, int> PaymentCompleted;

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
            PauseService pauseService,
            CustomerConfig customerConfig,
            int rewardXp,
            float interval,
            int maxActive)
        {
            stations = stationRegistry;
            economy = economyService;
            progression = progressionService;
            pause = pauseService;
            config = customerConfig;
            xpReward = rewardXp;
            spawnInterval = Mathf.Max(0.1f, interval);
            maximumActive = Mathf.Clamp(maxActive, 1, 8);
            spawnTimer = 0.25f;

            pool = new ObjectPool<CustomerAgent>(
                CreateCustomer,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPooledCustomer,
                true,
                maximumActive,
                maximumActive);
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
            if (!initialized || pause.IsPaused || !stations.IsOperational || activeCount >= maximumActive)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            spawnTimer = spawnInterval;
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
                stations,
                economy,
                progression,
                pause,
                exitPoint,
                xpReward,
                ReleaseCustomer,
                OnPaymentCompleted);
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
