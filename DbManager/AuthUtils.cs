using System.Security.Cryptography;

namespace DbManager;

public static class AuthUtils
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public static string GenerateSalt()
    {
        byte[] salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);
        return Convert.ToBase64String(salt);
    }

    public static string HashPassword(string password, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
        return Convert.ToBase64String(pbkdf2.GetBytes(HashSize));
    }

    public static bool VerifyPassword(string password, string salt, string storedHash)
        => HashPassword(password, salt) == storedHash;

    public static User? FindUser(string username)
        => Context.Instance.Users.FirstOrDefault(u => u.Username == username);

    public static User? CreateUser(string username, string password, string role = "user")
    {
        if (Context.Instance.Users.Any(u => u.Username == username))
            return null;

        string salt = GenerateSalt();
        string hash = HashPassword(password, salt);

        var user = new User
        {
            Username = username,
            PasswordHash = hash,
            Salt = salt,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        Context.Instance.Users.Add(user);
        Context.Instance.SaveChanges();
        return user;
    }
}
