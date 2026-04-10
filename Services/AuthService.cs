using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WealthTracker.Models;
using WealthTracker.Requests;

namespace WealthTracker.Services;

public class AuthService(ApplicationDbContext context, IConfiguration config) : IAuthService
{
    public async Task<string?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Password == password,cancellationToken);
        
        return user == null ? null : GenerateJwtToken(user);
    }

    public async Task<string?> RegisterAsync(RegisterRequest request,  CancellationToken cancellationToken = default)
    {
        var alreadyExists = await context.Users.AnyAsync(u => u.Email == request.Email);
        if (alreadyExists) return null;
        var newUser = new User()
        {
            FirstName =  request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = request.Password,
        }; 
        context.Users.Add(newUser);
        await context.SaveChangesAsync(cancellationToken);
        return GenerateJwtToken(newUser);
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