using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.ExceptionHandling;

/// <summary>
/// Builds the standardized <see cref="ProblemDetails"/> response for a missing order,
/// keeping the shape consistent with <see cref="GlobalExceptionHandler"/>'s error responses
/// instead of the empty body that <c>Results.NotFound()</c> would produce.
/// </summary>
internal static class OrderNotFoundProblem
{
    /// <summary>Builds the 404 result for the given request.</summary>
    /// <param name="httpContext">The current HTTP context, used to correlate the trace id.</param>
    internal static IResult Result(HttpContext httpContext) =>
        Results.Problem(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Order not found",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        });
}
