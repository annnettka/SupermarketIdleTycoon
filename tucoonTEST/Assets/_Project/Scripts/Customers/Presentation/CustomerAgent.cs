using SupermarketTycoon.Buildings;
using SupermarketTycoon.Checkout;
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

        private ICustomerState state;
        private bool warnedAboutNavMesh;

        internal CustomerRuntimeContext Context { get; private set; }
        internal ShelfStation Shelf { get; private set; }
        internal CheckoutStation Checkout { get; private set; }
        internal float PaymentMultiplier { get; private set; } = 1f;

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

        public void Configure(NavMeshAgent agent, CustomerVisualSelector visuals = null)
        {
            navigationAgent = agent;
            visualSelector = visuals;
        }

        public void Begin(CustomerRuntimeContext runtimeContext)
        {
            Context = runtimeContext;
            warnedAboutNavMesh = false;
            Shelf = null;
            Checkout = null;
            PaymentMultiplier = Context.ProfilePaymentMultiplier;

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
        }

        private void Reset()
        {
            navigationAgent = GetComponent<NavMeshAgent>();
            visualSelector = GetComponent<CustomerVisualSelector>();
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

        internal void CaptureShelfValue()
        {
            PaymentMultiplier = Context.ProfilePaymentMultiplier * (Shelf != null ? Shelf.IncomeMultiplier : 1f);
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
