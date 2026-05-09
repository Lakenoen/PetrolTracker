using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PetrolTracker.Pages;

public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("confirm_email");
        Response.Cookies.Delete("confirm_deadline");
        return RedirectToPage("/Login");
    }
}
