using PetrolTracker.Models;
using PetrolTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DbManager;

namespace PetrolTracker.Pages;

public class LoginModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtHelper _jwt;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<LoginModel> _logger;
    private readonly Context _ctx;
    private readonly IConfiguration _configuration;
    public LoginModel(
        UserManager<AppUser> userManager,
        JwtHelper jwt,
        IEmailSender emailSender,
        ILogger<LoginModel> logger,
        IConfiguration configuration)
    {
        _configuration = configuration;
        _userManager = userManager;
        _jwt = jwt;
        _emailSender = emailSender;
        _logger = logger;
        _ctx = new Context(new Settings
        {
            UpdateDB = false,
            ConnectionDB = _configuration["Dbconnect"]!
        });
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

        var dbUser = DbApi.FindUserByEmail(_ctx, Input.Email);
        var user = await _userManager.FindByEmailAsync(Input.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, Input.Password))
        {
            if(dbUser is null || !DbApi.CheckPassword(dbUser, Input.Password)){
                ErrorMessage = "Неверный email или пароль";
                return Page();
            }
        }

        if (user is not null && dbUser is null)
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

        if(user is null && dbUser is not null)
        {
            user = new AppUser { UserName = dbUser.Username, Email = dbUser.Email};
            var result = await _userManager.CreateAsync(user, Input.Password);
            if (!result.Succeeded)
            {
                ErrorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
                return Page();
            }
            await _userManager.AddToRoleAsync(user, "User");
        }

        var roles = new List<string>();
        roles.Add(dbUser!.Role);
        var token = _jwt.GenerateToken(user!, roles);

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
