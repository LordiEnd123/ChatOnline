using System.Collections.Concurrent;

namespace ChatServer.Models;

public static class UserStore
{
    // Потокобезопасный список пользователей в памяти
    private static readonly ConcurrentDictionary<string, User> _usersByEmail = new();

    // Немного тестовых пользователей, чтобы сразу было с кем чатиться
    static UserStore()
    {
        AddUser(new User
        {
            Email = "user1@mail.com",
            Password = "123",
            Name = "User 1",
            AvatarUrl = null,
            Status = UserStatus.Online
        });

        AddUser(new User
        {
            Email = "user2@mail.com",
            Password = "123",
            Name = "User 2",
            AvatarUrl = null,
            Status = UserStatus.Offline
        });
    }

    public static IEnumerable<User> GetAll() => _usersByEmail.Values;

    public static User? GetByEmail(string email)
        => _usersByEmail.TryGetValue(email.ToLower(), out var user) ? user : null;

    public static bool AddUser(User user)
    {
        return _usersByEmail.TryAdd(user.Email.ToLower(), user);
    }

    public static bool UpdateUser(User user)
    {
        _usersByEmail[user.Email.ToLower()] = user;
        return true;
    }
}
