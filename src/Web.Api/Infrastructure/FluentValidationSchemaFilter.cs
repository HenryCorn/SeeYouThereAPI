// <copyright file="FluentValidationSchemaFilter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using FluentValidation;
    using FluentValidation.Validators;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    /// <summary>
    /// Schema filter for FluentValidation rules to add validation constraints to Swagger.
    /// </summary>
    public class FluentValidationSchemaFilter : ISchemaFilter
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentValidationSchemaFilter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public FluentValidationSchemaFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == null)
            {
                return;
            }

            var validators = _serviceProvider.GetServices<IValidator>()
                .Where(v => v.GetType().BaseType?.GenericTypeArguments.FirstOrDefault() == context.Type);

            foreach (var validator in validators)
            {
                ApplyValidatorRulesToSchema(schema, validator, context.Type);
            }
        }

        private void ApplyValidatorRulesToSchema(OpenApiSchema schema, IValidator validator, Type type)
        {
            var validatorOriginalType = validator.GetType();
            var rulesetProperties = validatorOriginalType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(IRuleBuilderOptions<,>));

            var validatorRules = validatorOriginalType.GetField("Rules", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(validator) as System.Collections.IEnumerable;

            if (validatorRules == null)
            {
                return;
            }

            foreach (var rule in validatorRules)
            {
                var ruleType = rule.GetType();
                var propertyName = ruleType.GetProperty("PropertyName")?.GetValue(rule) as string;

                if (string.IsNullOrEmpty(propertyName) || !schema.Properties.ContainsKey(propertyName))
                {
                    continue;
                }

                var validators = ruleType.GetProperty("Validators")?.GetValue(rule) as System.Collections.IEnumerable;
                if (validators == null)
                {
                    continue;
                }

                foreach (var propertyValidator in validators)
                {
                    var validatorType = propertyValidator.GetType();

                    if (validatorType.Name.Contains("NotEmptyValidator"))
                    {
                        schema.Properties[propertyName].Nullable = false;
                        if (!schema.Required.Contains(propertyName))
                        {
                            schema.Required.Add(propertyName);
                        }
                    }
                    else if (validatorType.Name.Contains("LengthValidator"))
                    {
                        var min = validatorType.GetProperty("Min")?.GetValue(propertyValidator) as int?;
                        var max = validatorType.GetProperty("Max")?.GetValue(propertyValidator) as int?;

                        if (min.HasValue)
                        {
                            schema.Properties[propertyName].MinLength = min;
                        }

                        if (max.HasValue)
                        {
                            schema.Properties[propertyName].MaxLength = max;
                        }
                    }
                    else if (validatorType.Name.Contains("GreaterThanOrEqualValidator"))
                    {
                        if (schema.Properties[propertyName].Type == "number" ||
                            schema.Properties[propertyName].Type == "integer")
                        {
                            var valueToCompare = validatorType.GetProperty("ValueToCompare")?.GetValue(propertyValidator);
                            if (valueToCompare != null)
                            {
                                schema.Properties[propertyName].Minimum = Convert.ToDecimal(valueToCompare);
                            }
                        }
                    }
                    else if (validatorType.Name.Contains("RegularExpressionValidator"))
                    {
                        var expression = validatorType.GetProperty("Expression")?.GetValue(propertyValidator) as string;
                        if (!string.IsNullOrEmpty(expression))
                        {
                            schema.Properties[propertyName].Pattern = expression;
                        }
                    }
                    else if (validatorType.Name.Contains("EnumValidator"))
                    {
                        var enumValues = validatorType.GetProperty("EnumValues")?.GetValue(propertyValidator) as System.Collections.IEnumerable;
                        if (enumValues != null)
                        {
                            var enumList = new List<IOpenApiAny>();
                            foreach (var value in enumValues)
                            {
                                enumList.Add(new OpenApiString(value.ToString()));
                            }

                            schema.Properties[propertyName].Enum = enumList;
                        }
                    }
                }
            }
        }
    }
}
