using System.Text;
using System.Text.Json;
using DbManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PetrolTracker.Pages
{
    [IgnoreAntiforgeryToken]
    [Authorize]
    public class IndexModel : PageModel
    {
        public List<GasStation>? GasStations { get; set; } = null;
        public List<Petrol>? Petrols { get; set; } = null;
        public (double min, double max) PriceRange {get;set;} = (0,100);
        
        private readonly Context _ctx;
        private readonly IConfiguration _configuration;
        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _ctx = new Context(new Settings
            {
                UpdateDB = false,
                ConnectionDB = _configuration["Dbconnect"]!
            });
        }
        public string GetUpdate(GasStation station, Petrol petrol) => DbApi.GetUpdate(station, petrol).ToString("dd/MM/yyyy");
        public IActionResult OnGet()
        {
            PriceRange = DbApi.getPetrolPriceRange(_ctx);
            Petrols = DbApi.GetAllPetrols(_ctx);
            Filter? filter = null;
            if (HttpContext.Request.Query["filter"].Count > 0)
                filter = JsonSerializer.Deserialize<Filter>(HttpContext.Request.Query["filter"]!);

            if (!long.TryParse(HttpContext.Request.Query["page"], out long page))
                page = 0;

            GasStations = DbApi.GetStations(_ctx, filter, page, 100);
            
            return Page();
        }

    }
}