using SupermarketTycoon.Buildings;
using SupermarketTycoon.Checkout;
using SupermarketTycoon.Products;
using UnityEngine;
using UnityEngine.AI;

namespace SupermarketTycoon.Customers
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CustomerAgent : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent navigationAgent;
        [SerializeField] private CustomerVisualSelector visualSelector;
        [SerializeField] private ProductCarryView productCarryView;

        private ICustomerState state;
        private bool warnedAboutNavMesh;

        internal CustomerRuntimeContext Context { get; private set; }
        internal ShelfStation Shelf { get; private set; }
        internal CheckoutStation Checkout { get; private set; }
        internal float PaymentMultiplier { get; private set; } = 1f;
        public ProductDefinition CarriedProduct => productCarryView != null ? productCarryView.ActiveProduct : null;

        internal bool HasArrived
        {
            get
            {
                if (!CanNavigate || navigationAgent.pathPending)
                {
                    return false;
                }

                return navigationAgent.remainingDistance <= navigationAgent.stoppingDistance + 0.05f;
            }
        }

        public void Configure(
            NavMeshAgent agent,
            CustomerVisualSelector visuals = null,
            ProductCarryView carryView = null)
        {
            navigationAgent = agent;
            visualSelector = visuals;
            productCarryView = carryView;
        }

        public void Begin(CustomerRuntimeContext runtimeContext)
        {
            Context = runtimeContext;
            warnedAboutNavMesh = false;
            Shelf = null;
            Checkout = null;
            PaymentMultiplier = Context.ProfilePaymentMultiplier;
            productCarryView?.Clear();

            navigationAgent.speed = Context.Config.MovementSpeed * Context.MovementSpeedMultiplier;
            navigationAgent.acceleration = Context.Config.Acceleration;
            navigationAgent.angularSpeed = Context.Config.AngularSpeed;
            navigationAgent.stoppingDistance = Context.Config.StoppingDistance;
            visualSelector?.Select(
                Context.Progression.CurrentLevel,
                Context.Profile != null ? Context.Profile.PresentationColor : Color.white);
            ChangeState(new CustomerSpawnState(this));
        }

        public void PrepareForPool()
        {
            state?.Exit();
            state = null;
            ReleaseShelf();
            LeaveCheckout();
            productCarryView?.Clear();
            StopMoving();
            Context = null;
        }

        private bool CanNavigate =>
            navigationAgent != null &&
            navigationAgent.enabled &&
            navigationAgent.isOnNavMesh;

        private void Awake()
        {
            visualSelector ??= GetComponent<CustomerVisualSelector>();
            productCarryView ??= GetComponent<ProductCarryView>();
        }

        private void Reset()
        {
            navigationAgent = GetComponent<NavMeshAgent>();
            visualSelector = GetComponent<CustomerVisualSelector>();
            productCarryView = GetComponent<ProductCarryView>();
        }

        private void Update()
        {
            if (Context == null || state == null)
            {
                return;
            }

            if (Context.Pause.IsPaused)
            {
                StopMoving();
                return;
            }

            if (CanNavigate)
            {
                navigationAgent.isStopped = false;
            }

            state.Tick(Time.deltaTime);
        }

        internal void ChangeState(ICustomerState nextState)
        {
            state?.Exit();
            state = nextState;
            state?.Enter();
        }

        internal void MoveTo(Vector3 destination)
        {
            if (CanNavigate)
            {
                navigationAgent.isStopped = false;
                navigationAgent.SetDestination(destination);
                return;
            }

            if (!warnedAboutNavMesh)
            {
                Debug.LogWarning("Customer is waiting because it is not placed on a NavMesh.", this);
                warnedAboutNavMesh = true;
            }
        }

        internal void StopMoving()
        {
            if (CanNavigate)
            {
                navigationAgent.isStopped = true;
                navigationAgent.ResetPath();
            }
        }

        internal void SetShelf(ShelfStation shelf)
        {
            Shelf = shelf;
        }

        internal bool TryPickupProduct()
        {
            if (Shelf == null || !Shelf.TryTakeProduct(this, out var product) || product == null)
            {
                return false;
            }

            PaymentMultiplier = Context.ProfilePaymentMultiplier * Shelf.IncomeMultiplier * product.ValueMultiplier;
            productCarryView?.Show(product);
            return true;
        }

        internal void ClearCarriedProduct()
        {
            productCarryView?.Clear();
        }

        internal void ReleaseShelf()
        {
            if (Shelf != null)
            {
                Shelf.Release(this);
                Shelf = null;
            }
        }

        internal void SetCheckout(CheckoutStation checkout)
        {
            Checkout = checkout;
        }

        internal void CompleteCheckout()
        {
            if (Checkout != null)
            {
                Checkout.Complete(this);
                Checkout = null;
            }
        }

        internal void LeaveCheckout()
        {
            if (Checkout != null)
            {
                Checkout.Leave(this);
                Checkout = null;
            }
        }

    }
}
