﻿using Promethium.Plugin.Promotions.Extensions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Framework.Rules;
using System.Linq;

namespace Promethium.Plugin.Promotions.Actions
{
    /// <summary>
    /// A SiteCore Commerce action for the benefit
    /// "Get [specific amount] off the shipping cost"
    /// </summary>
    [EntityIdentifier("Pm_" + nameof(CartAmountOffShippingAction))]
    public class CartAmountOffShippingAction : ICartAction
    {
        public IRuleValue<decimal> Pm_SpecificAmount { get; set; }

        public void Execute(IRuleExecutionContext context)
        {
            //Get configuration
            var commerceContext = context.Fact<CommerceContext>();
            var cart = commerceContext?.GetObject<Cart>();
            if (cart == null || !cart.Lines.Any() || !cart.HasComponent<FulfillmentComponent>())
            {
                return;
            }

            var amountOff = Pm_SpecificAmount.Yield(context);
            if (amountOff == 0)
            {
                return;
            }

            //Get data
            var fulfillmentFee = GetFulfillmentFee(cart);
            if (fulfillmentFee == 0)
            {
                return;
            }

            if (amountOff > fulfillmentFee)
            {
                amountOff = fulfillmentFee;
            }

            //Apply action
            amountOff = amountOff.ShouldRoundPriceCalc(commerceContext);

            cart.Adjustments.AddCartLevelAwardedAdjustment(commerceContext, amountOff * -1, nameof(CartAmountOffShippingAction));

            cart.GetComponent<MessagesComponent>().AddPromotionApplied(commerceContext, nameof(CartAmountOffShippingAction));
        }

        private static decimal GetFulfillmentFee(Cart cart)
        {
            if (cart.HasComponent<SplitFulfillmentComponent>())
            {
                return cart.Lines
                    .Select(line => line.Adjustments.FirstOrDefault(a => a.Name.EqualsOrdinalIgnoreCase("FulfillmentFee")))
                    .TakeWhile(lineFulfillment => lineFulfillment != null)
                    .Sum(lineFulfillment => lineFulfillment.Adjustment.Amount);
            }

            var awardedAdjustment = cart.Adjustments.FirstOrDefault(a => a.Name.EqualsOrdinalIgnoreCase("FulfillmentFee"));
            return awardedAdjustment?.Adjustment.Amount ?? 0;
        }
    }
}