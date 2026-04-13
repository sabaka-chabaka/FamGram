using Microsoft.EntityFrameworkCore;

namespace FamGram.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatMember> ChatMembers => Set<ChatMember>();
    public DbSet<Message> Messages => Set<Message>();
 
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>().HasIndex(u => u.Username).IsUnique();
 
        mb.Entity<ChatMember>()
            .HasKey(cm => new { cm.ChatId, cm.UserId });
 
        mb.Entity<ChatMember>()
            .HasOne(cm => cm.Chat)
            .WithMany(c => c.Members)
            .HasForeignKey(cm => cm.ChatId);
 
        mb.Entity<ChatMember>()
            .HasOne(cm => cm.User)
            .WithMany(u => u.ChatMemberships)
            .HasForeignKey(cm => cm.UserId);
 
        mb.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.Messages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
 
        mb.Entity<Message>()
            .HasOne(m => m.Chat)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ChatId);
    }
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? AvatarColor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ChatMember> ChatMemberships { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}
 
public class Chat
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsGroup { get; set; }
    public string? AvatarColor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string EncryptedKey { get; set; } = "";
    public ICollection<ChatMember> Members { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}
 
public class ChatMember
{
    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
 
public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string EncryptedContent { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}