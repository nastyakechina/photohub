namespace PhotoHub.AuthService.Domain.Users;

public sealed class User
{
    private User()
    {
        UserName = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    private User(Guid id, string userName, string email, string passwordHash, DateTime createdAtUtc)
    {
        Id = id;
        UserName = userName;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string UserName { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static User Create(string userName, string email, string passwordHash)
    {
        return new User(
            Guid.NewGuid(),
            userName,
            email,
            passwordHash,
            DateTime.UtcNow);
    }
}
