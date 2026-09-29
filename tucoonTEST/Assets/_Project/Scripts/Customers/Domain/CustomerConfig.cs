using UnityEngine;

namespace SupermarketTycoon.Customers
{
    [CreateAssetMenu(menuName = "Supermarket Tycoon/Customer Config", fileName = "CustomerConfig")]
    public sealed class CustomerConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float movementSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float acceleration = 14f;
        [SerializeField, Min(1f)] private float angularSpeed = 720f;
        [SerializeField, Min(0.01f)] private float stoppingDistance = 0.15f;
        [SerializeField, Min(0.1f)] private float shoppingDuration = 2.25f;
        [SerializeField, Min(0.1f)] private float paymentDuration = 0.65f;
        [SerializeField, Min(0.1f)] private float retryDelay = 0.75f;

        public float MovementSpeed => movementSpeed;
        public float Acceleration => acceleration;
        public float AngularSpeed => angularSpeed;
        public float StoppingDistance => stoppingDistance;
        public float ShoppingDuration => shoppingDuration;
        public float PaymentDuration => paymentDuration;
        public float RetryDelay => retryDelay;
    }
}
