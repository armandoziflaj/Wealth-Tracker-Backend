using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WealthTracker.Responses;

namespace WealthTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseController : ControllerBase
{
    protected IActionResult CreateSuccessResponse<T>(T data)
    {
        var response = new BaseResponse<T>
        {
            Data = data,
            IsSuccess = true,
            Errors = []
        };
        return Ok(response);
    }

    protected IActionResult CreateErrorResponse<T>(List<string> errors, String message, int statusCode = 400)
    {
        var response = new BaseResponse<T>
        {
            Data = default,
            IsSuccess = false,
            Message = message, 
            Errors = errors
        };
        return StatusCode(statusCode, response);
    }
    public long GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
        return string.IsNullOrEmpty(userIdClaim) ? 0 : long.Parse(userIdClaim);
    }
}
