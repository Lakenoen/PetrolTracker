using PetrolTracker.Models;
using PetrolTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PetrolTracker.Pages;

public class LoginModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtHelper _jwt;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        UserManager<AppUser> userManager,
        JwtHelper jwt,
        IEmailSender emailSender,
        ILogger<LoginModel> logger)
    {
        _userManager = userManager;
        _jwt = jwt;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty]
    public LoginRequest Input { get; set; } = new("", "");

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, Input.Password))
        {
            ErrorMessage = "Неверный email или пароль";
            return Page();
        }

        if (!user.EmailConfirmed)
        {
            try
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _emailSender.SendAsync(user.Email!,
                    "Подтверждение регистрации",
                    $"Ваш код подтверждения: {code}\n\nКод действителен 10 минут.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось отправить письмо для {Email}", user.Email);
            }

            SetConfirmationCookies(user.Email!, SixDigitCodeTokenProvider.TokenLifespan);
            return RedirectToPage("/ConfirmEmail", new { email = user.Email });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwt.GenerateToken(user, roles);

        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return RedirectToPage("/Index");
    }

    private void SetConfirmationCookies(string email, TimeSpan ttl)
    {
        var deadline = DateTimeOffset.UtcNow.Add(ttl);
        var options = new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Expires = deadline };
        Response.Cookies.Append("confirm_email", email, options);
        Response.Cookies.Append("confirm_deadline", deadline.ToUnixTimeSeconds().ToString(), options);
    }
}
