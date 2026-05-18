using DbManager;
using PetrolTracker.Models;
using PetrolTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PetrolTracker.Pages;

public class RegisterModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<AppUser> userManager,
        IEmailSender emailSender,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty]
    public RegisterRequest Input { get; set; } = new("", "", "");

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (await _userManager.FindByEmailAsync(Input.Email) is not null)
        {
            ErrorMessage = "Пользователь с таким email уже зарегистрирован";
            return Page();
        }

        var user = new AppUser { UserName = Input.UserName, Email = Input.Email };
        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
            return Page();
        }

        await _userManager.AddToRoleAsync(user, "User");
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        SetConfirmationCookies(user.Email!, SixDigitCodeTokenProvider.TokenLifespan);

        try
        {
            await _emailSender.SendAsync(user.Email!,
                "Подтверждение регистрации",
                $"Ваш код подтверждения: {code}\n\nКод действителен 10 минут.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось отправить письмо для {Email}", user.Email);
        }

        return RedirectToPage("/ConfirmEmail", new { email = user.Email });
    }

    private void SetConfirmationCookies(string email, TimeSpan ttl)
    {
        var deadline = DateTimeOffset.UtcNow.Add(ttl);
        var options = new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Expires = deadline };
        Response.Cookies.Append("confirm_email", email, options);
        Response.Cookies.Append("confirm_deadline", deadline.ToUnixTimeSeconds().ToString(), options);
    }
}
