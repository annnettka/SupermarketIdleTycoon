using System.Collections;
using System.Collections.Generic;
using SupermarketTycoon.Customers;
using UnityEngine;

namespace SupermarketTycoon.Products
{
    /// <summary>
    /// Coordinates logical shelf slots, customer reservations, and staged visual restocking after depletion.
    /// Координирует логические слоты полки, резервирования покупателей и поэтапное визуальное пополнение после опустошения.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShelfStockView : MonoBehaviour
    {
        [SerializeField] private ShelfProductSlot[] slots;
        [SerializeField, Min(0.1f)] private float restockDelay = 4.5f;
        [SerializeField] private bool premium;

        private readonly Dictionary<CustomerAgent, ShelfProductSlot> reservations = new();
        private int unlockedSlotCount = 3;
        private float currentRestockDelay;

        public int AvailableCount
        {
            get
            {
                var count = 0;
                if (slots == null)
                {
                    return count;
                }

                for (var i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null && slots[i].IsAvailable)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int UnlockedSlotCount => unlockedSlotCount;
        public bool IsPremium => premium;

        public void Configure(ShelfProductSlot[] productSlots, float delay, bool isPremium = false)
        {
            slots = productSlots;
            restockDelay = Mathf.Max(0.1f, delay);
            premium = isPremium;
            currentRestockDelay = restockDelay;
            ApplyLevel(1);
        }

        public void ApplyLevel(int level)
        {
            if (slots == null)
            {
                return;
            }

            unlockedSlotCount = Mathf.Min(slots.Length, level <= 1 ? 3 : level == 2 ? 5 : 8);
            currentRestockDelay = restockDelay / (1f + Mathf.Max(0, level - 1) * 0.22f);
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i]?.SetUnlocked(i < unlockedSlotCount);
            }
        }

        public bool TryReserve(CustomerAgent customer)
        {
            RemoveMissingReservations();
            if (customer == null || reservations.ContainsKey(customer) || slots == null)
            {
                return false;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.TryReserve())
                {
                    reservations.Add(customer, slot);
                    return true;
                }
            }

            return false;
        }

        public bool TryTake(CustomerAgent customer, out ProductDefinition product)
        {
            product = null;
            if (customer == null || !reservations.TryGetValue(customer, out var slot))
            {
                return false;
            }

            reservations.Remove(customer);
            product = slot != null ? slot.Consume() : null;
            if (product == null)
            {
                return false;
            }

            StartCoroutine(RestockAfterDelay(slot));
            return true;
        }

        public void CancelReservation(CustomerAgent customer)
        {
            if (customer == null || !reservations.TryGetValue(customer, out var slot))
            {
                return;
            }

            reservations.Remove(customer);
            slot?.CancelReservation();
        }

        public void ResetStock()
        {
            StopAllCoroutines();
            reservations.Clear();
            if (slots == null)
            {
                return;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                slots[i]?.ResetState();
                slots[i]?.SetUnlocked(i < unlockedSlotCount);
            }
        }

        private void Awake()
        {
            currentRestockDelay = restockDelay;
        }

        private IEnumerator RestockAfterDelay(ShelfProductSlot slot)
        {
            var stageDelay = currentRestockDelay / 3f;
            yield return new WaitForSeconds(stageDelay);
            slot?.SetFillState(ProductStockFill.Low);
            yield return new WaitForSeconds(stageDelay);
            slot?.SetFillState(ProductStockFill.Medium);
            yield return new WaitForSeconds(stageDelay);
            slot?.Restock();
        }

        private void RemoveMissingReservations()
        {
            if (reservations.Count == 0)
            {
                return;
            }

            var missing = ListPool<CustomerAgent>.Get();
            foreach (var pair in reservations)
            {
                if (pair.Key == null || !pair.Key.gameObject.activeInHierarchy)
                {
                    pair.Value?.CancelReservation();
                    missing.Add(pair.Key);
                }
            }

            for (var i = 0; i < missing.Count; i++)
            {
                reservations.Remove(missing[i]);
            }

            ListPool<CustomerAgent>.Release(missing);
        }

        /// <summary>
        /// Reuses short-lived candidate lists so reservation selection does not allocate during customer traffic.
        /// Переиспользует временные списки кандидатов, чтобы выбор резервирования не создавал память во время потока покупателей.
        /// </summary>
        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();

            public static List<T> Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new List<T>();
            }

            public static void Release(List<T> list)
            {
                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
