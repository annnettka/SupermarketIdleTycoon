using UnityEngine;

namespace SupermarketTycoon.Customers
{
    internal interface ICustomerState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }

    internal abstract class CustomerState : ICustomerState
    {
        protected CustomerState(CustomerAgent customer)
        {
            Customer = customer;
        }

        protected CustomerAgent Customer { get; }
        protected CustomerRuntimeContext Context => Customer.Context;

        public virtual void Enter()
        {
        }

        public abstract void Tick(float deltaTime);

        public virtual void Exit()
        {
        }
    }

    internal sealed class CustomerSpawnState : CustomerState
    {
        private float retryTimer;

        public CustomerSpawnState(CustomerAgent customer) : base(customer)
        {
        }

        public override void Enter()
        {
            Customer.StopMoving();
            retryTimer = 0f;
        }

        public override void Tick(float deltaTime)
        {
            retryTimer -= deltaTime;
            if (retryTimer > 0f)
            {
                return;
            }

            if (Context.Stations.TryReserveShelf(Customer, out var shelf))
            {
                Customer.SetShelf(shelf);
                Customer.ChangeState(new CustomerMoveToShelfState(Customer));
                return;
            }

            retryTimer = Context.Config.RetryDelay;
        }
    }

    internal sealed class CustomerMoveToShelfState : CustomerState
    {
        public CustomerMoveToShelfState(CustomerAgent customer) : base(customer)
        {
        }

        public override void Enter()
        {
            MoveToShelf();
        }

        public override void Tick(float deltaTime)
        {
            var shelf = Customer.Shelf;
            if (shelf == null)
            {
                Customer.ReleaseShelf();
                Customer.ChangeState(new CustomerSpawnState(Customer));
                return;
            }

            Customer.MoveTo(shelf.GetInteractionPosition(Customer));
            if (Customer.HasArrived)
            {
                Customer.ChangeState(new CustomerShoppingState(Customer));
            }
        }

        private void MoveToShelf()
        {
            if (Customer.Shelf != null)
            {
                Customer.MoveTo(Customer.Shelf.GetInteractionPosition(Customer));
            }
        }
    }

    internal sealed class CustomerShoppingState : CustomerState
    {
        private float remaining;

        public CustomerShoppingState(CustomerAgent customer) : base(customer)
        {
        }

        public override void Enter()
        {
            Customer.StopMoving();
            Customer.CaptureShelfValue();
            remaining = (Customer.Shelf != null
                ? Customer.Shelf.ShoppingDuration
                : Context.Config.ShoppingDuration) * Context.ShoppingTimeMultiplier;
        }

        public override void Tick(float deltaTime)
        {
            remaining -= deltaTime;
            if (remaining > 0f)
            {
                return;
            }

            Customer.ReleaseShelf();
            Customer.ChangeState(new CustomerMoveToCheckoutState(Customer));
        }
    }

    internal sealed class CustomerMoveToCheckoutState : CustomerState
    {
        private float retryTimer;

        public CustomerMoveToCheckoutState(CustomerAgent customer) : base(customer)
        {
        }

        public override void Enter()
        {
            Customer.StopMoving();
            retryTimer = 0f;
        }

        public override void Tick(float deltaTime)
        {
            retryTimer -= deltaTime;
            if (retryTimer > 0f)
            {
                return;
            }

            if (Context.Stations.TryJoinCheckout(Customer, out var checkout))
            {
                Customer.SetCheckout(checkout);
                Customer.ChangeState(new CustomerQueueState(Customer));
                return;
            }

            retryTimer = Context.Config.RetryDelay;
        }
    }

    internal sealed class CustomerQueueState : CustomerState
    {
        private float patienceRemaining;

        public CustomerQueueState(CustomerAgent customer) : base(customer)
        {
        }

        public override void Enter()
        {
            patienceRemaining = Context.QueuePatience;
        }

        public override void Tick(float deltaTime)
        {
            var checkout = Customer.Checkout;
            if (checkout == null)
            {
                Customer.LeaveCheckout();
                Customer.ChangeState(new CustomerMoveToCheckoutState(Customer));
                return;
            }

            patienceRemaining -= deltaTime;
            if (patienceRemaining <= 0f)
            {
                Customer.LeaveCheckout();
                Context.CustomerLost?.Invoke(Customer.transform.position);
                Customer.ChangeState(new CustomerExitState(Customer));
                return;
            }

            var target = checkout.GetQueuePoint(Customer);
            if (target == null)
            {
                Customer.LeaveCheckout();
                Customer.ChangeState(new CustomerMoveToCheckoutState(Customer));
                return;
            }

            Customer.MoveTo(target.position);
            if (checkout.IsFirst(Customer) && Customer.HasArrived)
            {
                Customer.ChangeState(new CustomerPayState(Customer));
            }
        }
    }

    internal sealed class CustomerPayState : CustomerState
    {
        private float remaining;

        public CustomerPayState(CustomerAgent customer) : base(customer)
        {
        }

        public override void Enter()
        {
            Customer.StopMoving();
            remaining = Customer.Checkout != null
                ? Customer.Checkout.ProcessingDuration
                : Context.Config.PaymentDuration;
        }

        public override void Tick(float deltaTime)
        {
            if (Customer.Checkout == null || !Customer.Checkout.IsFirst(Customer))
            {
                Customer.LeaveCheckout();
                Customer.ChangeState(new CustomerMoveToCheckoutState(Customer));
                return;
            }

            remaining -= deltaTime;
            if (remaining > 0f)
            {
                return;
            }

            var amount = Context.Economy.AddCustomerIncome(Customer.PaymentMultiplier);
            Context.Progression.AddXp(Context.XpReward);
            Context.PaymentCompleted?.Invoke(Customer.transform.position, amount);
            Customer.CompleteCheckout();
            Customer.ChangeState(new CustomerExitState(Customer));
        }
    }

    internal sealed class CustomerExitState : CustomerState
    {
        public CustomerExitState(CustomerAgent customer) : base(customer)
        {
        }

        public override void Enter()
        {
            Customer.MoveTo(Context.ExitPoint.position);
        }

        public override void Tick(float deltaTime)
        {
            if (Context.ExitPoint == null)
            {
                Context.Release(Customer);
                return;
            }

            Customer.MoveTo(Context.ExitPoint.position);
            if (Customer.HasArrived)
            {
                Context.Release(Customer);
            }
        }
    }
}
