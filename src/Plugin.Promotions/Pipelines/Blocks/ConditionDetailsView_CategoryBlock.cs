﻿using Promethium.Plugin.Promotions.Extensions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Management;
using Sitecore.Commerce.Plugin.Search;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Promethium.Plugin.Promotions.Factory;

namespace Promethium.Plugin.Promotions.Pipelines.Blocks
{
    public class ConditionDetailsView_CategoryBlock : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        private readonly GetCategoryCommand _getCategoryCommand;
        private readonly SitecoreConnectionManager _manager;

        public ConditionDetailsView_CategoryBlock(GetCategoryCommand getCategoryCommand, SitecoreConnectionManager manager)
        {
            _getCategoryCommand = getCategoryCommand;
            _manager = manager;
        }

        public override async Task<EntityView> Run(EntityView arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull(arg.Name + ": The argument cannot be null");

            var entity = arg.Properties.FirstOrDefault(p => p.Name.EqualsOrdinalIgnoreCase("Condition") || p.Name.EqualsOrdinalIgnoreCase("Action"));
            if (entity == null || !entity.RawValue.ToString().StartsWith("Pm_") || !entity.RawValue.ToString().Contains("InCategory"))
            {
                return arg;
            }

            var categorySelection = arg.Properties.FirstOrDefault(x => x.Name.EqualsOrdinalIgnoreCase("Pm_SpecificCategory"));
            if (categorySelection == null)
            {
                return arg;
            }

            var policyByType = SearchScopePolicy.GetPolicyByType(context.CommerceContext, context.CommerceContext.Environment, typeof(Category));
            if (policyByType == null)
            {
                return arg;
            }

            var policy = new Policy()
            {
                PolicyId = "EntityType",
                Models = new List<Model> { new Model { Name = nameof(Category) } }
            };
            categorySelection.UiType = "Autocomplete";
            categorySelection.Policies.Add(policy);
            categorySelection.Policies.Add(policyByType);

            await AddReadOnlyFullPath(arg, context, categorySelection);

            return arg;
        }

        private async Task AddReadOnlyFullPath(EntityView arg, CommercePipelineExecutionContext context, ViewProperty categorySelection)
        {
            if (categorySelection.RawValue != null &&
                !string.IsNullOrEmpty(categorySelection.RawValue.ToString()) &&
                categorySelection.RawValue.ToString().IndexOf("-Category-", StringComparison.OrdinalIgnoreCase) > 0)
            {
                var categoryFactory = new CategoryFactory(context.CommerceContext, _manager, _getCategoryCommand);
                var readOnlyProp = new ViewProperty
                {
                    DisplayName = $"Full category path of '{categorySelection.RawValue}'",
                    IsHidden = false,
                    IsReadOnly = true,
                    Name = "FullCategoryPath",
                    IsRequired = false,
                    OriginalType = "System.String",
                    Value = await categoryFactory.GetCategoryPath(categorySelection.RawValue.ToString()),
                };

                arg.Properties.Insert(arg.Properties.IndexOf(categorySelection) + 1, readOnlyProp);
            }
        }
    }
}