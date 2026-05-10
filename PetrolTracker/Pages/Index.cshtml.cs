using DbManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PetrolTracker.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public List<GasStation>? GasStations { get; set; } = null;

        public string GetUpdate(GasStation station, Petrol petrol) => Utils.GetUpdate(station, petrol).ToString("dd/MM/yyyy");
        public void OnGet()
        {
            if(long.TryParse(HttpContext.Request.Query["page"], out long page))
            {
                GasStations = DbManager.Utils.GetGasTations(page, 100);
                return;
            }

            GasStations = DbManager.Utils.GetGasTations(0, 100);
        }
    }
}
