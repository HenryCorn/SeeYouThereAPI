// <copyright file="DestinationsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Org.OpenAPITools.Controllers;
using Org.OpenAPITools.Models;

/// <inheritdoc />
public class DestinationsController : DestinationsApiController
{
    /// <inheritdoc/>
    public override IActionResult FindCheapestDestination(CheapestDestinationRequest cheapestDestinationRequest)
    {
        throw new NotImplementedException();
    }
}