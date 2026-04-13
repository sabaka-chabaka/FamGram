using System.Security.Claims;
using FamGram.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamGram.Controllers;
 
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = await db.Users.FindAsync(UserId);
        if (user is null) return NotFound();
        return Ok(new { user.Id, user.Username, user.DisplayName, user.AvatarColor });
    }
 
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(Array.Empty<object>());
 
        var users = await db.Users
            .Where(u => u.Id != UserId &&
                        (u.Username.Contains(q.ToLower()) || u.DisplayName.ToLower().Contains(q.ToLower())))
            .Take(20)
            .Select(u => new { u.Id, u.Username, u.DisplayName, u.AvatarColor })
            .ToListAsync();
 
        return Ok(users);
    }
}