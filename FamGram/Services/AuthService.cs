using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FamGram.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FamGram.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    private readonly string _jwtKey = config["Jwt:Key"];
    
    public async Task<(User? user, string? error)> RegisterAsync(string username, string displayName, string password)
    {
        if (await db.Users.AnyAsync(u => u.Username == username))
            return (null, "Username already taken");
 
        var colors = new[] { "#3b82f6", "#8b5cf6", "#ec4899", "#f59e0b", "#10b981", "#ef4444" };
        var user = new User
        {
            Username    = username.ToLower().Trim(),
            DisplayName = displayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            AvatarColor = colors[Random.Shared.Next(colors.Length)]
        };
 
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (user, null);
    }
    
    public async Task<(User? user, string? error)> LoginAsync(string username, string password)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username.ToLower().Trim());
        if (user is null) return (null, "User not found");
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return (null, "Wrong password");
        return (user, null);
    }
    
    public string GenerateToken(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
 
        var token = new JwtSecurityToken(
            claims: [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("display_name", user.DisplayName)
            ],
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds
        );
 
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}