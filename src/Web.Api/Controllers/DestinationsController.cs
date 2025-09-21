// Copyright (c) SeeYouThere. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using Org.OpenAPITools.Controllers;
using Org.OpenAPITools.Models;

namespace Web.Api.Controllers;

/// <inheritdoc />
public class DestinationsController : DestinationsApiController
{
    /// <inheritdoc/>
    public override IActionResult FindCheapestDestination(CheapestDestinationRequest cheapestDestinationRequest)
    {
        throw new NotImplementedException();
    }
}