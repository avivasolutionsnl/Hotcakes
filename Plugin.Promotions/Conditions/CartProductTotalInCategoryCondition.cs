﻿using Promethium.Plugin.Promotions.Classes;
using Promethium.Plugin.Promotions.Factory;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Rules;
using System.Linq;

namespace Promethium.Plugin.Promotions.Conditions
{
    /// <summary>
    /// A Sitecore Commerce condition for the qualification
    /// "Cart contains products in the [specific category] for a total [compares] [specific value]"
    /// </summary>
    [EntityIdentifier("Pm_" + nameof(CartProductTotalInCategoryCondition))]
    public class CartProductTotalInCategoryCondition : ICartsCondition
    {
        private readonly GetCategoryCommand _getCategoryCommand;

        public CartProductTotalInCategoryCondition(GetCategoryCommand getCategoryCommand)
        {
            _getCategoryCommand = getCategoryCommand;
        }

        public IRuleValue<string> Pm_SpecificCategory { get; set; }

        public IBinaryOperator<decimal, decimal> Pm_Compares { get; set; }

        public IRuleValue<decimal> Pm_SpecificValue { get; set; }

        public IRuleValue<bool> Pm_IncludeSubCategories { get; set; }

        public bool Evaluate(IRuleExecutionContext context)
        {
            var commerceContext = context.Fact<CommerceContext>();

            //Get configuration
            var specificCategory = Pm_SpecificCategory.Yield(context);
            var specificValue = Pm_SpecificValue.Yield(context);
            var includeSubCategories = Pm_IncludeSubCategories.Yield(context);
            if (string.IsNullOrEmpty(specificCategory) || specificValue == 0 || Pm_Compares == null)
            {
                return false;
            }

            //Get data
            var categoryFactory = new CategoryFactory(commerceContext, null, _getCategoryCommand);
            var categorySitecoreId = AsyncHelper.RunSync(() => categoryFactory.GetSitecoreIdFromCommerceId(specificCategory));

            var cartLineFactory = new CartLineFactory(commerceContext);
            var categoryLines = cartLineFactory.GetLinesMatchingCategory(categorySitecoreId, includeSubCategories);
            if (categoryLines == null)
            {
                return false;
            }

            //Validate data against configuration
            var categoryTotal = categoryLines.Sum(line => line.Totals.GrandTotal.Amount);
            return Pm_Compares.Evaluate(categoryTotal, specificValue);
        }
    }
}
