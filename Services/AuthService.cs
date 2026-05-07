using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using WealthTracker.Models;
using WealthTracker.Requests;

namespace WealthTracker.Services;

public class AuthService(ApplicationDbContext context, IConfiguration config) : IAuthService
{
    public async Task<string?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken: cancellationToken);
        if (user == null) return null;
        
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
        
        return !isPasswordValid ? null : GenerateJwtToken(user);
    }

    public async Task<string?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        request.Email = request.Email.ToUpperInvariant();
        
        var alreadyExists = await context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken: cancellationToken);
        if (alreadyExists) return null;
        var newUser = new User()
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };
        context.Users.Add(newUser);
        await context.SaveChangesAsync(cancellationToken);
        return GenerateJwtToken(newUser);
    }

    public async Task<string?> LoginGoogle(string googleToken, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new HttpClient();
        
            var response = await client.GetAsync(
                $"https://www.googleapis.com/oauth2/v3/userinfo?access_token={googleToken}", 
                cancellationToken);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var googleUser = System.Text.Json.JsonSerializer.Deserialize<GoogleUserDto>(json);

            if (googleUser == null || string.IsNullOrEmpty(googleUser.email)) return null;

            var normalizedEmail = googleUser.email.ToUpperInvariant();
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (user != null) return GenerateJwtToken(user);
            
            user = new User
            {
                Email = normalizedEmail,
                Username = normalizedEmail,
                FirstName = googleUser.given_name,
                LastName = googleUser.family_name,
                Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);

            return GenerateJwtToken(user);
        }
        catch
        {
            return null;
        }
    }

private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(config["Jwt:Key"] ?? "MySuperSecretKey1234567890");
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            ]),
            Expires = DateTime.UtcNow.AddHours(3),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
public class GoogleUserDto
{
    public string email { get; set; } = null!;
    public string given_name { get; set; } = null!;
    public string family_name { get; set; } = null!;
}