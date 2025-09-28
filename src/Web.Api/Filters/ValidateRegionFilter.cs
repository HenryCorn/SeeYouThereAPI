using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Org.OpenAPITools.Models;

namespace Web.Api.Filters
{
    /// <summary>
    /// Action filter that validates regions in cheapest destination requests.
    /// </summary>
    public class ValidateRegionFilter : IAsyncActionFilter
    {
        private readonly IRegionValidator _regionValidator;
        private readonly ILogger<ValidateRegionFilter> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateRegionFilter"/> class.
        /// </summary>
        /// <param name="regionValidator">The region validator.</param>
        /// <param name="logger">The logger.</param>
        public ValidateRegionFilter(IRegionValidator regionValidator, ILogger<ValidateRegionFilter> logger)
        {
            _regionValidator = regionValidator ?? throw new ArgumentNullException(nameof(regionValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes the action filter to validate regions in the request.
        /// </summary>
        /// <param name="context">The action executing context.</param>
        /// <param name="next">The action execution delegate.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.ActionArguments.TryGetValue("cheapestDestinationRequest", out var requestObj) &&
                requestObj is CheapestDestinationRequest request)
            {
                // Validate the regions based on the region type
                if (request.Regions != null && request.Regions.Any())
                {
                    RegionType regionType = request.RegionType switch
                    {
                        CheapestDestinationRequest.RegionTypeEnum.ContinentEnum => RegionType.Continent,
                        CheapestDestinationRequest.RegionTypeEnum.CountryEnum => RegionType.Country,
                        CheapestDestinationRequest.RegionTypeEnum.CitiesEnum => RegionType.City,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    bool isValid = _regionValidator.ValidateRegionCodes(
                        regionType,
                        request.Regions,
                        out List<string> invalidCodes);

                    if (!isValid)
                    {
                        _logger.LogWarning("Invalid region codes detected: {InvalidCodes}",
                            string.Join(", ", invalidCodes));

                        var problemDetails = new ProblemDetails
                        {
                            Status = 400,
                            Title = "Invalid Region Codes",
                            Detail = $"The following region codes are invalid for type '{regionType}': {string.Join(", ", invalidCodes)}",
                            Type = "https://seeyouthere.api/errors/validation/invalid-region-codes"
                        };

                        context.Result = new BadRequestObjectResult(problemDetails);
                        return;
                    }
                }

                // Validate the origin airport codes
                if (request.Origins != null && request.Origins.Any())
                {
                    var invalidOrigins = new List<string>();

                    foreach (var origin in request.Origins)
                    {
                        if (!_regionValidator.IsValidCityCode(origin))
                        {
                            invalidOrigins.Add(origin);
                        }
                    }

                    if (invalidOrigins.Any())
                    {
                        _logger.LogWarning("Invalid origin airport codes detected: {InvalidOrigins}",
                            string.Join(", ", invalidOrigins));

                        var problemDetails = new ProblemDetails
                        {
                            Status = 400,
                            Title = "Invalid Origin Codes",
                            Detail = $"The following origin codes are not valid IATA airport codes: {string.Join(", ", invalidOrigins)}",
                            Type = "https://seeyouthere.api/errors/validation/invalid-origin-codes"
                        };

                        context.Result = new BadRequestObjectResult(problemDetails);
                        return;
                    }
                }
            }

            // If validation passed, continue with the pipeline
            await next();
        }
    }
}
