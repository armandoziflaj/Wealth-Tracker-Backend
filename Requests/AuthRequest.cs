namespace WealthTracker.Requests;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public required string FirstName { get; set; } 
    public required string LastName { get; set; } 
    public required string Email { get; set; } 
    public required string Password { get; set; } 
}