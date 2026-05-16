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
        public List<StationInfo>? GasStations { get; set; } = null;
        public List<Petrol>? Petrols { get; set; } = null;

        public string GetUpdate(GasStation station, Petrol petrol) => Utils.GetUpdate(station, petrol).ToString("dd/MM/yyyy");
        public IActionResult OnGet()
        {
            Petrols = Utils.GetAllPetrols();
            Filter? filter = null;
            if (HttpContext.Request.Query["filter"].Count > 0)
                filter = JsonSerializer.Deserialize<Filter>(HttpContext.Request.Query["filter"]!);

            if (!long.TryParse(HttpContext.Request.Query["page"], out long page))
                page = 0;

            GasStations = DbManager.Utils.GetStationsInfo(filter, page, 100);
            
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var filter = await HttpContext.Request.ReadFromJsonAsync<Filter>();
            string json_filter = JsonSerializer.Serialize(filter);
            return RedirectToPage("Index", new { filter = json_filter });
        }
    }
}