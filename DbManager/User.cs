using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DbManager;

[Index("Username", IsUnique = true)]
[Index("Email", IsUnique = true)]
public class User
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string Salt {get;set;} = string.Empty;

    public string Role { get; set; } = "user";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
