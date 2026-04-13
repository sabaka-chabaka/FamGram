using FamGram.Data;
using Microsoft.EntityFrameworkCore;

namespace FamGram.Services;

public class MessageService(AppDbContext db, EncryptionService enc)
{
    public async Task<MessageDto?> SendAsync(int chatId, int senderId, string plaintext)
    {
        var chatKey = enc.UnwrapChatKey(
            (await db.Chats.FindAsync(chatId))?.EncryptedKey ?? throw new InvalidOperationException("Chat not found"));
 
        var encrypted = enc.EncryptMessage(plaintext, chatKey);
 
        var msg = new Message
        {
            ChatId           = chatId,
            SenderId         = senderId,
            EncryptedContent = encrypted,
            SentAt           = DateTime.UtcNow
        };
 
        db.Messages.Add(msg);
        await db.SaveChangesAsync();
 
        await db.Entry(msg).Reference(m => m.Sender).LoadAsync();
        return ToDto(msg, plaintext);
    }
    
    public async Task<List<MessageDto>> GetMessagesAsync(int chatId, int userId, int skip = 0, int take = 50)
    {
        var chat = await db.Chats.FindAsync(chatId);
        if (chat is null) return [];
 
        var chatKey = enc.UnwrapChatKey(chat.EncryptedKey);
 
        var messages = await db.Messages
            .Include(m => m.Sender)
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
 
        messages.Reverse();
 
        return messages.Select(m =>
        {
            string plain;
            try { plain = enc.DecryptMessage(m.EncryptedContent, chatKey); }
            catch { plain = "[decryption failed]"; }
            return ToDto(m, plain);
        }).ToList();
    }
 
    private static MessageDto ToDto(Message m, string plain) =>
        new(m.Id, m.ChatId, m.SenderId, m.Sender.DisplayName, m.Sender.AvatarColor, plain, m.SentAt, m.IsRead);
}
 
public record MessageDto(int Id, int ChatId, int SenderId, string SenderName,
    string? SenderAvatar, string Content, DateTime SentAt, bool IsRead);