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

        private ICustomerState state;
        private bool warnedAboutNavMesh;

        internal CustomerRuntimeContext Context { get; private set; }
        internal ShelfStation Shelf { get; private set; }
        internal CheckoutStation Checkout { get; private set; }

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

        public void Configure(NavMeshAgent agent)
        {
            navigationAgent = agent;
        }

        public void Begin(CustomerRuntimeContext runtimeContext)
        {
            Context = runtimeContext;
            warnedAboutNavMesh = false;
            Shelf = null;
            Checkout = null;

            navigationAgent.speed = Context.Config.MovementSpeed;
            navigationAgent.acceleration = Context.Config.Acceleration;
            navigationAgent.angularSpeed = Context.Config.AngularSpeed;
            navigationAgent.stoppingDistance = Context.Config.StoppingDistance;
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

        private void Reset()
        {
            navigationAgent = GetComponent<NavMeshAgent>();
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
