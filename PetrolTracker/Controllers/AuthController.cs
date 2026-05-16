using PetrolTracker.Models;
using PetrolTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PetrolTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtHelper _jwt;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<AppUser> userManager,
        JwtHelper jwt,
        IEmailSender emailSender,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _jwt = jwt;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return BadRequest(new { message = "Пользователь с таким email уже зарегистрирован" });

        var user = new AppUser { UserName = request.UserName, Email = request.Email };
        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(new { message = string.Join("; ", createResult.Errors.Select(e => e.Description)) });

        await _userManager.AddToRoleAsync(user, "User");
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        try
        {
            await _emailSender.SendAsync(user.Email!,
                "Подтверждение регистрации",
                $"Ваш код подтверждения: {code}\n\nКод действителен 10 минут.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось отправить письмо для {Email}", user.Email);
            return StatusCode(500, new { message = "Аккаунт создан, но письмо не отправлено." });
        }

        return Ok(new { message = "Регистрация успешна. Код подтверждения отправлен на email." });
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return BadRequest(new { message = "Пользователь не найден" });
        if (user.EmailConfirmed) return Ok(new { message = "Email уже подтверждён" });

        var result = await _userManager.ConfirmEmailAsync(user, request.Code);
        if (!result.Succeeded) return BadRequest(new { message = "Неверный или истёкший код" });

        return Ok(new { message = "Email успешно подтверждён." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Неверный email или пароль" });

        if (!user.EmailConfirmed)
            return StatusCode(403, new { message = "Email не подтверждён." });

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwt.GenerateToken(user, roles);

        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return Ok(new AuthResponse(token, user.UserName ?? "", user.Email ?? "", roles));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token");
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var name = User.Identity?.Name;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        return Ok(new { username = name, email, role });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = _userManager.Users.ToList();
        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new { u.Id, u.UserName, u.Email, u.EmailConfirmed, u.CreatedAt, Roles = roles });
        }
        return Ok(result);
    }
}
