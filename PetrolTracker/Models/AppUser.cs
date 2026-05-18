namespace PetrolTracker.Models;

// DTO-записи для запросов аутентификации
public record RegisterRequest(string Email, string UserName, string Password);
public record LoginRequest(string Email, string Password);
public record ConfirmEmailRequest(string Email, string Code);
public record ResendConfirmationRequest(string Email);
public record AuthResponse(string Token, string UserName, string Email, IList<string> Roles);
