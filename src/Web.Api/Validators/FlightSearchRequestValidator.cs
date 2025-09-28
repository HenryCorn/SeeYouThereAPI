// <copyright file="FlightSearchRequestValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Validators
{
    using Core.Models;
    using FluentValidation;

    /// <summary>
    /// Validator for FlightSearchRequest.
    /// </summary>
    public class FlightSearchRequestValidator : AbstractValidator<FlightSearchRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlightSearchRequestValidator"/> class.
        /// </summary>
        public FlightSearchRequestValidator()
        {
            RuleFor(x => x.Origin)
                .NotEmpty().WithMessage("Origin is required.")
                .Length(3).WithMessage("Origin must be 3 characters long (IATA airport code).");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Currency is required.")
                .Length(3).WithMessage("Currency must be 3 characters long (ISO 4217 currency code).");

            RuleFor(x => x.DepartureDate)
                .Must(date => date >= DateTime.Today)
                .WithMessage("Departure date must be today or in the future.");

            When(x => x.ReturnDate.HasValue, () =>
            {
                RuleFor(x => x.ReturnDate.Value)
                    .GreaterThanOrEqualTo(x => x.DepartureDate)
                    .WithMessage("Return date must be on or after departure date.");
            });

            // Validate that at least one filter is provided, if any
            When(x => !string.IsNullOrEmpty(x.ContinentFilter) || !string.IsNullOrEmpty(x.CountryFilter) ||
                      (x.DestinationListFilter != null && x.DestinationListFilter.Any()), () =>
            {
                // Either continent, country, or destination list should be provided, not multiple
                RuleFor(x => new { x.ContinentFilter, x.CountryFilter, x.DestinationListFilter })
                    .Must(x =>
                        (string.IsNullOrEmpty(x.ContinentFilter) ? 0 : 1) +
                        (string.IsNullOrEmpty(x.CountryFilter) ? 0 : 1) +
                        ((x.DestinationListFilter == null || !x.DestinationListFilter.Any()) ? 0 : 1) <= 1)
                    .WithMessage("Only one filter type (continent, country, or destination list) can be specified.");
            });

            When(x => x.DestinationListFilter != null && x.DestinationListFilter.Any(), () =>
            {
                RuleForEach(x => x.DestinationListFilter)
                    .NotEmpty().WithMessage("Destination codes cannot be empty.")
                    .Length(3).WithMessage("Destination codes must be 3 characters long (IATA airport code).");
            });
        }
    }
}
