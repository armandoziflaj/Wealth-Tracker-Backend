using Microsoft.AspNetCore.Mvc;
using WealthTracker.Models;
using WealthTracker.Requests;
using WealthTracker.Services;

namespace WealthTracker.Controllers;


public class AuthController(IAuthService authService) : BaseController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest user, CancellationToken cancellationToken = default)
    { 
        if (!ModelState.IsValid)
            return BadRequest("Invalid data");

        var result = await authService.RegisterAsync(user, cancellationToken);

        return (result is not null) 
            ? Success(result)
            : BadRequest("User already exists");
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        var token = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        
        return token == null ? Invalid( "Invalid email or password") 
                             : Success(new { Token = token });
    }
}