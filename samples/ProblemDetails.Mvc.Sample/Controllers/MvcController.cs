using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ProblemDetails.Mvc.Sample.Controllers;

[Route("mvc")]
[ApiController]
public class MvcController : ControllerBase
{
    [HttpGet("status/{statusCode}")]
    public ActionResult Status([FromRoute] int statusCode)
    {
        return StatusCode(statusCode);
    }

    [HttpGet("error")]
    public ActionResult Error()
    {
        throw new NotImplementedException("This is an exception thrown from an MVC controller.");
    }

    [HttpGet("modelstate")]
    public ActionResult InvalidModelState([Required, FromQuery] string asdf)
    {
        return Ok();
    }

    [HttpGet("error/details")]
    public ActionResult ErrorDetails()
    {
        ModelState.AddModelError("someProperty", "This property failed validation.");

        var validation = new ValidationProblemDetails(ModelState)
        {
            Status = StatusCodes.Status422UnprocessableEntity
        };

        throw new ProblemDetailsException(validation);
    }

    [HttpGet("detail")]
    public ActionResult<string> Detail()
    {
        return BadRequest("This will end up in the 'detail' field.");
    }

    [HttpGet("result")]
    public ActionResult<OutOfCreditProblemDetails> Result()
    {
        var problem = new OutOfCreditProblemDetails
        {
            Type = "https://example.com/probs/out-of-credit",
            Title = "You do not have enough credit.",
            Detail = "Your current balance is 30, but that costs 50.",
            Instance = "/account/12345/msgs/abc",
            Balance = 30.0m,
            Accounts = { "/account/12345", "/account/67890" }
        };

        return BadRequest(problem);
    }

}
