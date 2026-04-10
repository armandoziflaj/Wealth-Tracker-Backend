using WealthTracker.Requests;

namespace WealthTracker.Services;

public interface IAuthService
{
    Task<string?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<string?> RegisterAsync(RegisterRequest user,  CancellationToken cancellationToken = default);
}