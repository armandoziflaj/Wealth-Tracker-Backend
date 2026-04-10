using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WealthTracker.Responses;

namespace WealthTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseController : ControllerBase
{
    protected IActionResult Success<T>(T data) 
        => Ok(BaseResponse<T>.Success(data));
    

    protected IActionResult BadRequest(string error, int statusCode = 400) 
        => StatusCode(statusCode, BaseResponse<object>.Failure(error));
    protected IActionResult NotFound(string error, int statusCode = 404) 
        => StatusCode(statusCode, BaseResponse<object>.Failure(error));

    protected IActionResult Invalid(string error = "Unauthorized") 
        => StatusCode(401, BaseResponse<object>.Failure(error));
    [NonAction]
    protected long GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
        return string.IsNullOrEmpty(userIdClaim) ? 0 : long.Parse(userIdClaim);
    }
}
