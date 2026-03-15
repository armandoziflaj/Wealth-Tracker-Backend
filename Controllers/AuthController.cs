using Microsoft.AspNetCore.Mvc;
using WealthTracker.Models;
using WealthTracker.Requests;
using WealthTracker.Services;

namespace WealthTracker.Controllers;


public class AuthController(IAuthService authService) : BaseController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest user)
    { 
        if (!ModelState.IsValid)
            return CreateErrorResponse<string>(["Invalid user data"], "Validation Error");

        var result = await authService.RegisterAsync(user);
        
        return (result is not null) 
            ? CreateSuccessResponse<string>(result) 
            : CreateErrorResponse<string>(["User already exists"], "User was not registered");
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var token = await authService.LoginAsync(request.Email, request.Password);
        
        return token == null ? CreateErrorResponse<string>(["Invalid email or password"], "Unauthorized", 401) 
                             : CreateSuccessResponse(new { Token = token });
    }
}