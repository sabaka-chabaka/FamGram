using System.Security.Claims;
using FamGram.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamGram.Controllers;

[ApiController]
[Route("api/chats")]
[Authorize]
public class ChatsController(ChatService chats, MessageService msgs) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    [HttpGet]
    public async Task<IActionResult> GetMyChats() =>
        Ok(await chats.GetUserChatsAsync(UserId));
 
    [HttpPost("direct")]
    public async Task<IActionResult> StartDirect([FromBody] DirectChatRequest req)
    {
        var chat = await chats.GetOrCreateDirectChatAsync(UserId, req.UserId);
        if (chat is null) return NotFound(new { error = "User not found" });
        var list = await chats.GetUserChatsAsync(UserId);
        return Ok(list.First(c => c.Id == chat.Id));
    }
    
    [HttpPost("group")]
    public async Task<IActionResult> CreateGroup([FromBody] GroupChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name required" });
        var chat = await chats.CreateGroupAsync(req.Name, UserId, req.MemberIds ?? []);
        if (chat is null) return BadRequest(new { error = "Failed to create group" });
        var list = await chats.GetUserChatsAsync(UserId);
        return Ok(list.First(c => c.Id == chat.Id));
    }
 
    [HttpGet("{chatId}/messages")]
    public async Task<IActionResult> GetMessages(int chatId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        if (!await chats.IsMemberAsync(chatId, UserId))
            return Forbid();
        return Ok(await msgs.GetMessagesAsync(chatId, UserId, skip, take));
    }
 
    [HttpPost("{chatId}/messages")]
    public async Task<IActionResult> SendMessage(int chatId, [FromBody] SendMessageRequest req)
    {
        if (!await chats.IsMemberAsync(chatId, UserId))
            return Forbid();
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(new { error = "Message cannot be empty" });
 
        var msg = await msgs.SendAsync(chatId, UserId, req.Content);
        return Ok(msg);
    }
}

public record DirectChatRequest(int UserId);
public record GroupChatRequest(string Name, List<int>? MemberIds);
public record SendMessageRequest(string Content);