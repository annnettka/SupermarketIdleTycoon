using System.Collections.Generic;
using SupermarketTycoon.Customers;
using SupermarketTycoon.Products;
using UnityEngine;

namespace SupermarketTycoon.Buildings
{
    /// <summary>
    /// Owns shelf capacity and customer reservations, coordinating logical stock with shopping interactions.
    /// Владеет вместимостью полки и резервированиями покупателей, связывая логический запас с покупками.
    /// </summary>
    public sealed class ShelfStation : MonoBehaviour
    {
        [SerializeField] private Transform interactionPoint;
        [SerializeField, Min(0.1f)] private float shoppingDuration = 2.25f;
        [SerializeField] private ShelfStockView stockView;

        private readonly List<CustomerAgent> reservations = new List<CustomerAgent>(3);
        private int capacity = 1;

        public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
        public float ShoppingDuration => shoppingDuration;
        public ShelfStockView StockView => stockView;
        public bool IsPremium => stockView != null && stockView.IsPremium;
        public float IncomeMultiplier { get; private set; } = 1f;
        public bool IsAvailable
        {
            get
            {
                RemoveMissingCustomers();
                return reservations.Count < capacity && (stockView == null || stockView.AvailableCount > 0);
            }
        }

        public void Configure(Transform point, float duration, ShelfStockView stock = null)
        {
            interactionPoint = point;
            shoppingDuration = Mathf.Max(0.1f, duration);
            stockView = stock;
        }

        public void ApplyLevel(BuildingLevelDefinition level)
        {
            capacity = level.Capacity;
            shoppingDuration = level.InteractionDuration;
            IncomeMultiplier = level.IncomeMultiplier;
            stockView?.ApplyLevel(level.Level);
        }

        public Vector3 GetInteractionPosition(CustomerAgent customer)
        {
            RemoveMissingCustomers();
            var index = Mathf.Max(0, reservations.IndexOf(customer));
            var centeredOffset = (index - (reservations.Count - 1) * 0.5f) * 0.55f;
            return InteractionPoint.position + InteractionPoint.right * centeredOffset;
        }

        /// <summary>
        /// Atomically reserves both shelf capacity and one logical product slot for a customer.
        /// Атомарно резервирует для покупателя вместимость полки и один логический слот товара.
        /// </summary>
        public bool TryReserve(CustomerAgent customer)
        {
            RemoveMissingCustomers();
            if (customer == null || reservations.Contains(customer) || reservations.Count >= capacity)
            {
                return false;
            }

            if (stockView != null && !stockView.TryReserve(customer))
            {
                return false;
            }

            reservations.Add(customer);
            return true;
        }

        /// <summary>
        /// Commits the customer's reserved logical product and starts presentation-only restocking.
        /// Подтверждает зарезервированный покупателем логический товар и запускает только визуальное пополнение.
        /// </summary>
        public bool TryTakeProduct(CustomerAgent customer, out ProductDefinition product)
        {
            if (stockView == null)
            {
                product = null;
                return false;
            }

            return stockView.TryTake(customer, out product);
        }

        public void Release(CustomerAgent customer)
        {
            reservations.Remove(customer);
            stockView?.CancelReservation(customer);
        }

        private void RemoveMissingCustomers()
        {
            for (var i = reservations.Count - 1; i >= 0; i--)
            {
                if (reservations[i] == null || !reservations[i].gameObject.activeInHierarchy)
                {
                    reservations.RemoveAt(i);
                }
            }
        }
    }
}
