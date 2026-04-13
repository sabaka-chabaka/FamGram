using FamGram.Data;
using Microsoft.EntityFrameworkCore;

namespace FamGram.Services;

public class ChatService(AppDbContext db, EncryptionService enc)
{
    public async Task<Chat?> GetOrCreateDirectChatAsync(int userAId, int userBId)
    {
        var existing = await db.Chats
            .Include(c => c.Members)
            .Where(c => !c.IsGroup &&
                        c.Members.Any(m => m.UserId == userAId) &&
                        c.Members.Any(m => m.UserId == userBId))
            .FirstOrDefaultAsync();
 
        if (existing is not null) return existing;
 
        var userB = await db.Users.FindAsync(userBId);
        if (userB is null) return null;
 
        var chatKey = enc.GenerateChatKey();
        var chat = new Chat
        {
            Name         = userB.DisplayName,
            IsGroup      = false,
            AvatarColor  = userB.AvatarColor,
            EncryptedKey = enc.WrapChatKey(chatKey),
        };
 
        db.Chats.Add(chat);
        await db.SaveChangesAsync();
 
        db.ChatMembers.AddRange(
            new ChatMember { ChatId = chat.Id, UserId = userAId },
            new ChatMember { ChatId = chat.Id, UserId = userBId }
        );
        await db.SaveChangesAsync();
        return chat;
    }
    
    public async Task<Chat?> CreateGroupAsync(string name, int creatorId, List<int> memberIds)
    {
        var chatKey = enc.GenerateChatKey();
        var chat = new Chat
        {
            Name         = name,
            IsGroup      = true,
            EncryptedKey = enc.WrapChatKey(chatKey),
            AvatarColor  = "#8b5cf6",
        };
 
        db.Chats.Add(chat);
        await db.SaveChangesAsync();
 
        var allMembers = memberIds.Union([creatorId]).Distinct();
        db.ChatMembers.AddRange(allMembers.Select(uid => new ChatMember { ChatId = chat.Id, UserId = uid }));
        await db.SaveChangesAsync();
        return chat;
    }
    
    public async Task<List<ChatDto>> GetUserChatsAsync(int userId)
    {
        var chats = await db.Chats
            .Include(c => c.Members).ThenInclude(m => m.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .ThenInclude(m => m.Sender)
            .Where(c => c.Members.Any(m => m.UserId == userId))
            .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.SentAt) ?? c.CreatedAt)
            .ToListAsync();
 
        return chats.Select(c => ToDto(c, userId, enc)).ToList();
    }
    
    public async Task<bool> IsMemberAsync(int chatId, int userId) =>
        await db.ChatMembers.AnyAsync(cm => cm.ChatId == chatId && cm.UserId == userId);
 
    public async Task<byte[]?> GetChatKeyAsync(int chatId)
    {
        var chat = await db.Chats.FindAsync(chatId);
        if (chat is null) return null;
        return enc.UnwrapChatKey(chat.EncryptedKey);
    }
    
    private static ChatDto ToDto(Chat c, int userId, EncryptionService enc)
    {
        var lastMsg = c.Messages.FirstOrDefault();
        string? lastPreview = null;
        if (lastMsg is not null)
        {
            try
            {
                var chatKey = enc.UnwrapChatKey(c.EncryptedKey);
                lastPreview = enc.DecryptMessage(lastMsg.EncryptedContent, chatKey);
                if (lastPreview.Length > 40) lastPreview = lastPreview[..40] + "…";
            }
            catch { lastPreview = "🔒 encrypted"; }
        }
 
        string name = c.Name;
        string? avatarColor = c.AvatarColor;
        if (!c.IsGroup)
        {
            var other = c.Members.FirstOrDefault(m => m.UserId != userId)?.User;
            if (other is not null)
            {
                name = other.DisplayName;
                avatarColor = other.AvatarColor;
            }
        }
 
        return new ChatDto(c.Id, name, c.IsGroup, avatarColor, lastPreview,
            lastMsg?.SentAt, c.Members.Select(m => new MemberDto(m.UserId, m.User.DisplayName, m.User.AvatarColor)).ToList());
    }
}

public record ChatDto(int Id, string Name, bool IsGroup, string? AvatarColor,
    string? LastMessage, DateTime? LastMessageAt, List<MemberDto> Members);
public record MemberDto(int Id, string DisplayName, string? AvatarColor);