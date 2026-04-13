using System.Security.Claims;
using FamGram.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FamGram;

[Authorize]
public class ChatHub(ChatService chats, MessageService msgs) : Hub
{
    private int UserId => int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
 
    public override async Task OnConnectedAsync()
    {
        var userChats = await chats.GetUserChatsAsync(UserId);
        foreach (var chat in userChats)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{chat.Id}");
 
        await base.OnConnectedAsync();
    }
 
    public async Task JoinChat(int chatId)
    {
        if (await chats.IsMemberAsync(chatId, UserId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{chatId}");
    }
 
    public async Task SendMessage(int chatId, string content)
    {
        if (!await chats.IsMemberAsync(chatId, UserId)) return;
        if (string.IsNullOrWhiteSpace(content)) return;
 
        var msg = await msgs.SendAsync(chatId, UserId, content);
        if (msg is null) return;
 
        await Clients.Group($"chat-{chatId}").SendAsync("NewMessage", msg);
    }
}