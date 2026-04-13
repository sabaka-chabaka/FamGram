using FamGram.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamGram.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length < 3)
            return BadRequest(new { error = "Username must be at least 3 characters" });
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return BadRequest(new { error = "Password must be at least 6 characters" });
 
        var (user, error) = await auth.RegisterAsync(req.Username, req.DisplayName ?? req.Username, req.Password);
        if (error is not null) return BadRequest(new { error });
 
        var token = auth.GenerateToken(user!);
        return Ok(new { token, user = new { user!.Id, user.Username, user.DisplayName, user.AvatarColor } });
    }
 
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var (user, error) = await auth.LoginAsync(req.Username, req.Password);
        if (error is not null) return Unauthorized(new { error });
 
        var token = auth.GenerateToken(user!);
        return Ok(new { token, user = new { user!.Id, user.Username, user.DisplayName, user.AvatarColor } });
    }
}

public record RegisterRequest(string Username, string? DisplayName, string Password);
public record LoginRequest(string Username, string Password);