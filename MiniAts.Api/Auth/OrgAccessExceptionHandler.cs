using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MiniAts.Api.Auth;

/// <summary>
/// Maps a missing/invalid org claim to a clean 403 instead of the bare 500 a live
/// customer would otherwise see whenever a profile is incomplete.
/// </summary>
public class OrgAccessExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not OrgAccessDeniedException ex)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Not authorized for this organization.",
                Detail = ex.Message,
            },
            cancellationToken);

        return true;
    }
}
