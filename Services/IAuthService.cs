using WealthTracker.Requests;

namespace WealthTracker.Services;

public interface IAuthService
{
    Task<string?> LoginAsync(string email, string password);
    Task<string?> RegisterAsync(RegisterRequest user);
}