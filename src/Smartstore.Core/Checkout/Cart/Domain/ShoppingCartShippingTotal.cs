﻿using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents a calculated shopping cart shipping total.
    /// </summary>
    public partial class ShoppingCartShippingTotal
    {
        public static implicit operator Money?(ShoppingCartShippingTotal obj)
            => obj.ShippingTotal;

        /// <summary>
        /// Cart shipping total in the primary currency.
        /// </summary>
        public Money? ShippingTotal { get; init; }

        /// <summary>
        /// Applied discount.
        /// </summary>
        public Discount AppliedDiscount { get; init; }

        /// <summary>
        /// Tax rate.
        /// </summary>
        public decimal TaxRate { get; init; }

        /// <summary>
        /// Overrides default <see cref="object.ToString()"/>. Returns formatted <see cref="SubTotalWithDiscount"/>.
        /// </summary>
        public override string ToString()
            => ShippingTotal.HasValue ? ShippingTotal.ToString() : decimal.Zero.FormatInvariant();
    }
}
