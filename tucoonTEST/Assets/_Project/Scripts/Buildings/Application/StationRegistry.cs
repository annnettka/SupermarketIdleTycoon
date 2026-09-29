using System.Collections.Generic;
using SupermarketTycoon.Checkout;
using SupermarketTycoon.Customers;

namespace SupermarketTycoon.Buildings
{
    public sealed class StationRegistry
    {
        private readonly List<ShelfStation> shelves = new List<ShelfStation>();
        private readonly List<CheckoutStation> checkouts = new List<CheckoutStation>();

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
            }
        }

        public bool TryReserveShelf(CustomerAgent customer, out ShelfStation shelf)
        {
            for (var i = 0; i < shelves.Count; i++)
            {
                var candidate = shelves[i];
                if (candidate != null && candidate.TryReserve(customer))
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
