using Microsoft.AspNetCore.Mvc;
using Org.OpenAPITools.Controllers;
using Org.OpenAPITools.Models;

namespace Web.Api.Controllers;

public class DestinationsController: DestinationsApiController
{
    public override IActionResult FindCheapestDestination(CheapestDestinationRequest cheapestDestinationRequest)
    {
        throw new NotImplementedException();
    }
}