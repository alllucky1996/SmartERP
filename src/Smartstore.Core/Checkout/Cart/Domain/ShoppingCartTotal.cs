﻿using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents a calculated shopping cart total.
    /// </summary>
    public partial class ShoppingCartTotal
    {
        public static implicit operator Money?(ShoppingCartTotal obj)
            => obj.Total;

        /// <summary>
        /// Total amount of the shopping cart in the primary currency. <c>null</c> if the cart total couldn't be calculated yet.
        /// The total amount is rounded if rounding is enabled for <see cref="IWorkContext.WorkingCurrency"/>.
        /// </summary>
        public Money? Total { get; init; }

        /// <summary>
        /// The amount by which the total was rounded in the primary currency, if rounding to the nearest multiple of denomination 
        /// (cash rounding) is activated for the currency.
        /// </summary>
        /// <example>"Schweizer Rappenrundung" of 16.23 -> <see cref="Total"/> = 16.25 and <see cref="ToNearestRounding"/> = 0.02.</example>
        public Money ToNearestRounding { get; init; }

        /// <summary>
        /// The shipping amount. <c>null</c> if the shipping amount could not be calculated.
        /// </summary>
        public Money? ShippingAmount { get; init; }

        /// <summary>
        /// Additional payment fees.
        /// </summary>
        public Money PaymentFee { get; init; }

        /// <summary>
        /// Applied discount amount in the primary currency.
        /// </summary>
        public Money DiscountAmount { get; init; }

        /// <summary>
        /// Applied discount.
        /// </summary>
        public Discount AppliedDiscount { get; init; }

        /// <summary>
        /// Reward points to redeem
        /// </summary>
        public int RedeemedRewardPoints { get; init; }

        /// <summary>
        /// Reward points amount to redeem in the primary currency.
        /// </summary>
        public Money RedeemedRewardPointsAmount { get; init; }

        /// <summary>
        /// Credit balance in the primary currency.
        /// </summary>
        public Money CreditBalance { get; init; }

        /// <summary>
        /// Applied gift cards.
        /// </summary>
        public List<AppliedGiftCard> AppliedGiftCards { get; init; }

        /// <summary>
        /// Shopping cart line items. A line item represents a single line in the shopping cart.
        /// At the moment this is always a product. Bundle items are not included as line items.
        /// </summary>
        public List<ShoppingCartLineItem> LineItems { get; init; }

        /// <summary>
        /// Total converted from the primary currency.
        /// </summary>
        public ConvertedAmounts ConvertedAmount { get; init; }

        /// <summary>
        /// Overrides default <see cref="object.ToString()"/>. Returns formatted <see cref="Total"/>.
        /// </summary>
        public override string ToString()
            => Total?.ToString() ?? decimal.Zero.FormatInvariant();

        /// <summary>
        /// Represents amount of <see cref="ShoppingCartTotal.Total"/> and <see cref="ShoppingCartTotal.ToNearestRounding"/> converted to <see cref="IWorkContext.WorkingCurrency"/>.
        /// </summary>
        public class ConvertedAmounts
        {
            /// <summary>
            /// Shopping cart total amount converted to <see cref="IWorkContext.WorkingCurrency"/>. <c>null</c> if the cart total couldn't be calculated now.
            /// </summary>
            public Money? Total { get; init; }

            /// <summary>
            /// Amount by which the total was rounded converted to <see cref="IWorkContext.WorkingCurrency"/>, if rounding to the nearest multiple of denomination 
            /// (cash rounding) is activated for the currency.
            /// </summary>
            public Money ToNearestRounding { get; init; }
        }
    }
}