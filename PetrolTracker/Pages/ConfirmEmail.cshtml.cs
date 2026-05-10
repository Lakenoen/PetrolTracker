using PetrolTracker.Models;
using PetrolTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PetrolTracker.Pages;

public class ConfirmEmailModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly JwtHelper _jwt;
    private readonly ILogger<ConfirmEmailModel> _logger;

    public ConfirmEmailModel(
        UserManager<AppUser> userManager,
        IEmailSender emailSender,
        JwtHelper jwt,
        ILogger<ConfirmEmailModel> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _jwt = jwt;
        _logger = logger;
    }

    [BindProperty]
    public ConfirmEmailRequest Input { get; set; } = new("", "");

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public int SecondsLeft { get; set; }

    public void OnGet(string? email)
    {
        var initialEmail = !string.IsNullOrEmpty(email)
            ? email
            : Request.Cookies["confirm_email"] ?? "";
        Input = new ConfirmEmailRequest(initialEmail, "");
        SecondsLeft = ReadSecondsLeftFromCookie();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            ErrorMessage = "Пользователь не найден";
            SecondsLeft = ReadSecondsLeftFromCookie();
            return Page();
        }

        if (user.EmailConfirmed)
            return await IssueJwtAndRedirectAsync(user);

        var result = await _userManager.ConfirmEmailAsync(user, Input.Code);
        if (!result.Succeeded)
        {
            ErrorMessage = "Неверный или истёкший код. Запросите новый.";
            SecondsLeft = ReadSecondsLeftFromCookie();
            return Page();
        }

        ClearConfirmationCookies();
        return await IssueJwtAndRedirectAsync(user);
    }

    public async Task<IActionResult> OnPostResendAsync()
    {
        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            ErrorMessage = "Пользователь не найден";
            SecondsLeft = ReadSecondsLeftFromCookie();
            return Page();
        }

        if (user.EmailConfirmed)
            return await IssueJwtAndRedirectAsync(user);

        try
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailSender.SendAsync(user.Email!,
                "Подтверждение регистрации (повторно)",
                $"Ваш новый код: {code}\n\nКод действителен 10 минут.");

            SetConfirmationCookies(user.Email!, SixDigitCodeTokenProvider.TokenLifespan);
            SuccessMessage = "Новый код отправлен на email.";
            SecondsLeft = (int)SixDigitCodeTokenProvider.TokenLifespan.TotalSeconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend failed for {Email}", user.Email);
            ErrorMessage = "Не удалось отправить письмо.";
            SecondsLeft = ReadSecondsLeftFromCookie();
        }

        return Page();
    }

    private async Task<IActionResult> IssueJwtAndRedirectAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwt.GenerateToken(user, roles);

        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        ClearConfirmationCookies();
        return RedirectToPage("/Index");
    }

    private int ReadSecondsLeftFromCookie()
    {
        var raw = Request.Cookies["confirm_deadline"];
        if (string.IsNullOrEmpty(raw) || !long.TryParse(raw, out var unixDeadline)) return 0;
        var diff = unixDeadline - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return diff > 0 ? (int)diff : 0;
    }

    private void SetConfirmationCookies(string email, TimeSpan ttl)
    {
        var deadline = DateTimeOffset.UtcNow.Add(ttl);
        var options = new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Expires = deadline };
        Response.Cookies.Append("confirm_email", email, options);
        Response.Cookies.Append("confirm_deadline", deadline.ToUnixTimeSeconds().ToString(), options);
    }

    private void ClearConfirmationCookies()
    {
        Response.Cookies.Delete("confirm_email");
        Response.Cookies.Delete("confirm_deadline");
    }
}
