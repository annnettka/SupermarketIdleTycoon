using SupermarketTycoon.Customers;
using UnityEngine;

namespace SupermarketTycoon.Buildings
{
    public sealed class ShelfStation : MonoBehaviour
    {
        [SerializeField] private Transform interactionPoint;
        [SerializeField, Min(0.1f)] private float shoppingDuration = 2.25f;

        private CustomerAgent reservedBy;

        public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
        public float ShoppingDuration => shoppingDuration;
        public bool IsAvailable => reservedBy == null;

        public void Configure(Transform point, float duration)
        {
            interactionPoint = point;
            shoppingDuration = Mathf.Max(0.1f, duration);
        }

        public bool TryReserve(CustomerAgent customer)
        {
            if (customer == null || reservedBy != null)
            {
                return false;
            }

            reservedBy = customer;
            return true;
        }

        public void Release(CustomerAgent customer)
        {
            if (reservedBy == customer)
            {
                reservedBy = null;
            }
        }
    }
}
