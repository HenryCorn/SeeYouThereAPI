// <copyright file="CheapestDestinationRequestValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Validators
{
    using FluentValidation;
    using Org.OpenAPITools.Models;

    /// <summary>
    /// Validator for CheapestDestinationRequest.
    /// </summary>
    public class CheapestDestinationRequestValidator : AbstractValidator<CheapestDestinationRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CheapestDestinationRequestValidator"/> class.
        /// </summary>
        public CheapestDestinationRequestValidator()
        {
            RuleFor(x => x.Origins)
                .NotNull().WithMessage("Origins are required.")
                .Must(x => x != null && x.Count > 0).WithMessage("At least one origin must be specified.");

            RuleForEach(x => x.Origins)
                .NotEmpty().WithMessage("Origin codes cannot be empty.")
                .Length(3).WithMessage("Origin codes must be 3 characters long (IATA airport code).");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Date is required.")
                .Must(date => BeValidFutureDate(date)).WithMessage("Date must be a valid future date.");

            When(x => x.RegionType != null, () =>
            {
                RuleFor(x => x.RegionType)
                    .IsInEnum().WithMessage("RegionType must be one of: continent, country, cities.");
            });

            When(x => x.Regions != null && x.Regions.Count > 0, () =>
            {
                RuleFor(x => x.RegionType)
                    .NotEmpty().WithMessage("RegionType must be specified when Regions are provided.");

                RuleForEach(x => x.Regions)
                    .NotEmpty().WithMessage("Region codes cannot be empty.");
            });
        }

        private static bool BeValidFutureDate(DateOnly date)
        {
            return date >= DateOnly.FromDateTime(DateTime.UtcNow);
        }
    }
}
