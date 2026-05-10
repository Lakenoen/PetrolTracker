using System.Collections.Concurrent;
using System.Security.Cryptography;
using PetrolTracker.Models;
using Microsoft.AspNetCore.Identity;

namespace PetrolTracker.Services;

public class SixDigitCodeTokenProvider : IUserTwoFactorTokenProvider<AppUser>
{
    public static readonly TimeSpan TokenLifespan = TimeSpan.FromMinutes(10);

    private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _store = new();

    public async Task<string> GenerateAsync(string purpose, UserManager<AppUser> manager, AppUser user)
    {
        var userId = await manager.GetUserIdAsync(user);
        var code = GenerateRandomCode();
        var key = $"{purpose}:{userId}";
        _store[key] = (code, DateTime.UtcNow.Add(TokenLifespan));
        return code;
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<AppUser> manager, AppUser user)
    {
        var userId = await manager.GetUserIdAsync(user);
        var key = $"{purpose}:{userId}";

        if (!_store.TryGetValue(key, out var entry))
            return false;

        if (DateTime.UtcNow > entry.Expiry)
        {
            _store.TryRemove(key, out _);
            return false;
        }

        if (!string.Equals(entry.Code, token, StringComparison.Ordinal))
            return false;

        _store.TryRemove(key, out _);
        return true;
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<AppUser> manager, AppUser user)
        => Task.FromResult(true);

    private static string GenerateRandomCode()
    {
        var value = RandomNumberGenerator.GetInt32(100000, 1000000);
        return value.ToString();
    }
}
