using Microsoft.AspNetCore.Identity;

namespace DbManager;

public class AppUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
