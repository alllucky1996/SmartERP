﻿using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the minimum tier price and applies it if it is lower than the FinalPrice.
    /// Tier prices of bundle items are ignored if per-item pricing is activated for the bundle.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Default + 100)]
    public class TierPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var product = context.Product;
            var options = context.Options;
            var processTierPrices = !options.IgnoreTierPrices && !options.IgnoreDiscounts && product.HasTierPrices && context.BundleItem == null;

            if (processTierPrices)
            {
                var tierPrices = await context.GetTierPricesAsync();

                // Put minimum tier price to context because it's required for discount calculation.
                context.MinTierPrice = GetMinimumTierPrice(product, tierPrices, context.Quantity);

                if (context.Options.DetermineLowestPrice && !context.HasPriceRange)
                {
                    context.HasPriceRange = tierPrices.Any() && !(tierPrices.Count == 1 && tierPrices.First().Quantity <= 1);
                }
            }

            // Process the whole pipeline. We need the result of discount calculation.
            await next(context);

            if (processTierPrices && context.MinTierPrice.HasValue)
            {
                // Wrong result:
                //context.FinalPrice = Math.Min(context.FinalPrice, context.MinTierPrice.Value);

                // Apply the minimum tier price if it achieves a lower price than the discounted FinalPrice
                // but exclude additional charge from comparing.
                context.FinalPrice -= context.AdditionalCharge;

                if (context.MinTierPrice.Value < context.FinalPrice)
                {
                    context.FinalPrice = context.MinTierPrice.Value;

                    // Apply discount on minimum tier price (if any).
                    var discountOnTierPrice = context.CalculatedDiscounts
                        .Where(x => x.Origin == nameof(context.MinTierPrice))
                        .OrderByDescending(x => x.DiscountAmount)
                        .FirstOrDefault();

                    if (discountOnTierPrice != null)
                    {
                        context.AppliedDiscounts.Add(discountOnTierPrice.Discount);
                        context.DiscountAmount += discountOnTierPrice.DiscountAmount;
                        context.FinalPrice -= discountOnTierPrice.DiscountAmount;
                    }
                }

                context.FinalPrice += context.AdditionalCharge;
            }
        }

        protected virtual decimal? GetMinimumTierPrice(Product product, IEnumerable<TierPrice> tierPrices, int quantity)
        {
            decimal? result = null;
            var previousQty = 1;

            foreach (var tierPrice in tierPrices)
            {
                if (quantity < tierPrice.Quantity || tierPrice.Quantity < previousQty)
                {
                    continue;
                }

                if (tierPrice.CalculationMethod == TierPriceCalculationMethod.Fixed)
                {
                    result = tierPrice.Price;
                }
                else if (tierPrice.CalculationMethod == TierPriceCalculationMethod.Percental)
                {
                    result = product.Price - (product.Price / 100m * tierPrice.Price);
                }
                else
                {
                    result = product.Price - tierPrice.Price;
                }

                previousQty = tierPrice.Quantity;
            }

            return result;
        }
    }
}
