namespace ChatServer.Models;

public enum UserStatus
{
    Offline,
    Online,
    DoNotDisturb
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!; // для ДЗ можно без хэша
    public string Name { get; set; } = null!;

    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Offline;

    // Настройки уведомлений
    public bool NotificationsEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool BannerEnabled { get; set; } = true;
}
