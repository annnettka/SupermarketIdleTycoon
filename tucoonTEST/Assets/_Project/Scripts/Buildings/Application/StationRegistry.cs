using System.Collections.Generic;
using SupermarketTycoon.Checkout;
using SupermarketTycoon.Customers;

namespace SupermarketTycoon.Buildings
{
    /// <summary>
    /// Tracks operational shelves and checkouts and owns customer reservation and queue selection policy.
    /// Отслеживает работающие полки и кассы и владеет политикой выбора резервирования и очереди покупателей.
    /// </summary>
    public sealed class StationRegistry
    {
        private readonly List<ShelfStation> shelves = new List<ShelfStation>();
        private readonly List<CheckoutStation> checkouts = new List<CheckoutStation>();
        private float checkoutEmployeeSpeedMultiplier = 1f;

        public bool IsOperational => HasShelf && HasCheckout;
        public bool HasShelf => shelves.Exists(shelf => shelf != null);
        public bool HasCheckout => checkouts.Exists(checkout => checkout != null);

        public void Register(ShelfStation shelf)
        {
            if (shelf != null && !shelves.Contains(shelf))
            {
                shelves.Add(shelf);
            }
        }

        public void Register(CheckoutStation checkout)
        {
            if (checkout != null && !checkouts.Contains(checkout))
            {
                checkouts.Add(checkout);
                checkout.SetEmployeeSpeedMultiplier(checkoutEmployeeSpeedMultiplier);
            }
        }

        public void SetCheckoutEmployeeSpeedMultiplier(float multiplier)
        {
            checkoutEmployeeSpeedMultiplier = multiplier < 0.1f ? 0.1f : multiplier;
            for (var i = 0; i < checkouts.Count; i++)
            {
                if (checkouts[i] != null)
                {
                    checkouts[i].SetEmployeeSpeedMultiplier(checkoutEmployeeSpeedMultiplier);
                }
            }
        }

        public bool TryReserveShelf(CustomerAgent customer, out ShelfStation shelf)
        {
            var preferPremium = customer != null && customer.PremiumShelfPreference >= 0.5f;
            if (TryReserveShelf(customer, preferPremium, out shelf))
            {
                return true;
            }

            return TryReserveShelf(customer, !preferPremium, out shelf);
        }

        private bool TryReserveShelf(CustomerAgent customer, bool premium, out ShelfStation shelf)
        {
            for (var i = 0; i < shelves.Count; i++)
            {
                var candidate = shelves[i];
                if (candidate != null && candidate.IsPremium == premium && candidate.TryReserve(customer))
                {
                    shelf = candidate;
                    return true;
                }
            }

            shelf = null;
            return false;
        }

        public bool TryJoinCheckout(CustomerAgent customer, out CheckoutStation checkout)
        {
            for (var i = 0; i < checkouts.Count; i++)
            {
                var candidate = checkouts[i];
                if (candidate != null && candidate.TryJoin(customer))
                {
                    checkout = candidate;
                    return true;
                }
            }

            checkout = null;
            return false;
        }
    }
}
