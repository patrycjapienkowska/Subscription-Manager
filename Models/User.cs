using System.ComponentModel.DataAnnotations;

namespace LifeAdmin.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    /// <summary>Hash hasła (PasswordHasher – PBKDF2 z solą). Nigdy nie trzymamy hasła jawnie.</summary>
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public record AuthRequest(
    [property: Required, StringLength(50, MinimumLength = 3)] string Username,
    [property: Required, StringLength(100, MinimumLength = 6)] string Password);

public record UserInfoDto(int Id, string Username);
