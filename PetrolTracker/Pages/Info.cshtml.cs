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
    public class Info : PageModel
    {
        private readonly Context _ctx;
        private readonly IConfiguration _configuration;

        public GasStation? Station {get; private set;}
        public Info(IConfiguration configuration)
        {
            _configuration = configuration;
            _ctx = new Context(new Settings
            {
                UpdateDB = false,
                ConnectionDB = _configuration["Dbconnect"]!
            });
        }

        public IActionResult OnGet()
        {
            long id = -1;
            if(HttpContext.Request.Query["id"].Count >= 0 
                && long.TryParse(HttpContext.Request.Query["id"], out id))
            {
                Station = DbApi.GetStationById(_ctx, id);
            }
            else
            {
                return RedirectToPage("Index");
            }
            
            return Page();
        }

        public async Task<IActionResult> Post()
        {
            PostModel? json = await HttpContext.Request.ReadFromJsonAsync<PostModel>();
            if(json is null)
                return new JsonResult(new {status = "fail"});

            //TODO
            switch(json.Type)
            {
                case "station": ;break;
                case "petrol": ;break;
                default: return new JsonResult(new {status = "fail"});
            }
            
            return new JsonResult(new {status = "success"});
        }

        public class PostModel
        {
            public string? Type {get;set;}
            public string? Name {get;set;}
            public double? Price {get;set;}
            public long? Id {get;set;}
        }

    }
}