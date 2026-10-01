using UnityEngine;

namespace SupermarketTycoon.Customers
{
    // Customer flow: Spawn -> Shelf reservation -> Shopping -> Checkout queue -> Pay -> Exit -> Pool.
    // Поток покупателя: Появление -> Резерв полки -> Покупка -> Очередь -> Оплата -> Выход -> Пул.

    /// <summary>
    /// Defines the focused lifecycle contract implemented by every customer state.
    /// Определяет узкий контракт жизненного цикла, реализуемый каждым состоянием покупателя.
    /// </summary>
    internal interface ICustomerState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }

    /// <summary>
    /// Provides shared access to the agent and its immutable runtime context for concrete states.
    /// Предоставляет конкретным состояниям общий доступ к агенту и его неизменяемому контексту выполнения.
    /// </summary>
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

    /// <summary>
    /// Waits at spawn until a shelf can be reserved, then transfers that reservation to movement.
    /// Ожидает у точки появления доступную полку, резервирует ее и передает резерв состоянию движения.
    /// </summary>
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

    /// <summary>
    /// Moves toward the reserved shelf; loss of the station releases ownership and retries from spawn state.
    /// Двигается к зарезервированной полке; потеря станции освобождает владение и возвращает к поиску.
    /// </summary>
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

    /// <summary>
    /// Runs the profile-adjusted shopping timer, consumes reserved logical stock, and releases the shelf.
    /// Выполняет скорректированный профилем таймер покупки, забирает зарезервированный запас и освобождает полку.
    /// </summary>
    internal sealed class CustomerShoppingState : CustomerState
    {
        private float remaining;

        public CustomerShoppingState(CustomerAgent customer) : base(customer)
        {
        }

        public override void Enter()
        {
            Customer.StopMoving();
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

            if (!Customer.TryPickupProduct())
            {
                Customer.ReleaseShelf();
                Customer.ChangeState(new CustomerSpawnState(Customer));
                return;
            }

            Customer.ReleaseShelf();
            Customer.ChangeState(new CustomerMoveToCheckoutState(Customer));
        }
    }

    /// <summary>
    /// Retains the carried product while retrying until an operational checkout queue accepts the customer.
    /// Сохраняет переносимый товар и повторяет попытки, пока рабочая очередь кассы не примет покупателя.
    /// </summary>
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

    /// <summary>
    /// Follows checkout-owned queue positions and either reaches payment or leaves after patience expires.
    /// Следует позициям очереди, которыми владеет касса, и переходит к оплате либо уходит по истечении терпения.
    /// </summary>
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
                Customer.ClearCarriedProduct();
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

    /// <summary>
    /// Processes only the queue head, commits income and XP once, clears the product, and releases the checkout.
    /// Обслуживает только первого в очереди, один раз начисляет доход и опыт, убирает товар и освобождает кассу.
    /// </summary>
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
            Customer.ClearCarriedProduct();
            Customer.CompleteCheckout();
            Customer.ChangeState(new CustomerExitState(Customer));
        }
    }

    /// <summary>
    /// Moves the ownership-free customer to the exit and returns the agent to its pool on arrival.
    /// Ведет покупателя без занятых ресурсов к выходу и по прибытии возвращает агента в пул.
    /// </summary>
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
