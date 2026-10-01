using System.Collections.Generic;
using SupermarketTycoon.Buildings;
using SupermarketTycoon.Customers;
using UnityEngine;

namespace SupermarketTycoon.Checkout
{
    /// <summary>
    /// Owns ordered checkout queue membership, positions, capacity, and employee-adjusted processing duration.
    /// Владеет порядком очереди кассы, позициями, вместимостью и скоростью обслуживания с учетом сотрудника.
    /// </summary>
    public sealed class CheckoutStation : MonoBehaviour
    {
        [SerializeField] private Transform[] queuePoints;

        private readonly List<CustomerAgent> queue = new List<CustomerAgent>(5);
        private int queueCapacity = 3;
        private float baseProcessingDuration = 2.5f;
        private float employeeSpeedMultiplier = 1f;

        public bool HasSpace
        {
            get
            {
                RemoveMissingCustomers();
                return queuePoints != null && queue.Count < Mathf.Min(queueCapacity, queuePoints.Length);
            }
        }

        public int Count => queue.Count;
        public int QueueCapacity => queueCapacity;
        public float ProcessingDuration => baseProcessingDuration / Mathf.Max(0.1f, employeeSpeedMultiplier);

        public void Configure(Transform[] points)
        {
            queuePoints = points;
        }

        public void ApplyLevel(BuildingLevelDefinition level)
        {
            queueCapacity = level.Capacity;
            baseProcessingDuration = level.InteractionDuration;
        }

        public void SetEmployeeSpeedMultiplier(float multiplier)
        {
            employeeSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        /// <summary>
        /// Appends a customer only when queue ownership and configured physical capacity permit it.
        /// Добавляет покупателя только тогда, когда владение очередью и физическая вместимость это позволяют.
        /// </summary>
        public bool TryJoin(CustomerAgent customer)
        {
            RemoveMissingCustomers();
            if (customer == null || queue.Contains(customer) || !HasSpace)
            {
                return false;
            }

            queue.Add(customer);
            return true;
        }

        public bool IsFirst(CustomerAgent customer)
        {
            RemoveMissingCustomers();
            return queue.Count > 0 && queue[0] == customer;
        }

        public Transform GetQueuePoint(CustomerAgent customer)
        {
            RemoveMissingCustomers();
            var index = queue.IndexOf(customer);
            if (index < 0 || queuePoints == null || queuePoints.Length == 0)
            {
                return null;
            }

            return queuePoints[Mathf.Min(index, queuePoints.Length - 1)];
        }

        /// <summary>
        /// Removes only the current queue head, preserving first-in-first-out checkout semantics.
        /// Удаляет только первого в очереди, сохраняя порядок обслуживания FIFO.
        /// </summary>
        public void Complete(CustomerAgent customer)
        {
            if (IsFirst(customer))
            {
                queue.RemoveAt(0);
            }
        }

        public void Leave(CustomerAgent customer)
        {
            queue.Remove(customer);
        }

        private void RemoveMissingCustomers()
        {
            for (var i = queue.Count - 1; i >= 0; i--)
            {
                if (queue[i] == null || !queue[i].gameObject.activeInHierarchy)
                {
                    queue.RemoveAt(i);
                }
            }
        }
    }
}
