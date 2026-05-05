using EmployeeOrderApi.Services.Common;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeOrderApi.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return result.Error.StatusCode switch
        {
            400 => new BadRequestObjectResult(new ProblemDetail(result.Error)),
            401 => new UnauthorizedObjectResult(new ProblemDetail(result.Error)),
            403 => new ObjectResult(new ProblemDetail(result.Error)) { StatusCode = 403 },
            404 => new NotFoundObjectResult(new ProblemDetail(result.Error)),
            409 => new ConflictObjectResult(new ProblemDetail(result.Error)),
            _   => new ObjectResult(new ProblemDetail(result.Error)) { StatusCode = result.Error.StatusCode }
        };
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(new { message = "Operation completed successfully." });

        return result.Error.StatusCode switch
        {
            400 => new BadRequestObjectResult(new ProblemDetail(result.Error)),
            401 => new UnauthorizedObjectResult(new ProblemDetail(result.Error)),
            403 => new ObjectResult(new ProblemDetail(result.Error)) { StatusCode = 403 },
            404 => new NotFoundObjectResult(new ProblemDetail(result.Error)),
            409 => new ConflictObjectResult(new ProblemDetail(result.Error)),
            _   => new ObjectResult(new ProblemDetail(result.Error)) { StatusCode = result.Error.StatusCode }
        };
    }
}

public record ProblemDetail(string Code, string Message)
{
    public ProblemDetail(Error error) : this(error.Code, error.Description) { }
}
