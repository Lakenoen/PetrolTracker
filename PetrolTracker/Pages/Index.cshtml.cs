using System.Text;
using System.Text.Json;
using DbManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PetrolTracker.Pages
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        public List<GasStation>? GasStations { get; set; } = null;

        public string GetUpdate(GasStation station, Petrol petrol) => Utils.GetUpdate(station, petrol).ToString("dd/MM/yyyy");
        public void OnGet()
        {
            if(long.TryParse(HttpContext.Request.Query["page"], out long page))
            {
                GasStations = DbManager.Utils.GetGasTations(null, page, 100);
                return;
            }

            GasStations = DbManager.Utils.GetGasTations(null, 0, 100);
        }

        public async Task OnPost()
        {
            var filter = await HttpContext.Request.ReadFromJsonAsync<Filter>();
            GasStations = DbManager.Utils.GetGasTations(filter, 0, 100);
        }
    }
}
