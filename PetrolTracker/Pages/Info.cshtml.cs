using System.Text;
using System.Text.Json;
using DbManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PetrolTracker.Models;

namespace PetrolTracker.Pages
{
    [IgnoreAntiforgeryToken]
    [Authorize]
    public class Info : PageModel
    {
        public List<string> PetrolNames {get;}= new List<string>()
        {
            "АИ-92",
            "АИ-95",
            "АИ-95+",
            "ДТ",
            "АИ-100"
        };
        private readonly Context _ctx;
        private readonly IConfiguration _configuration;
        private User? _user;
        private readonly UserManager<AppUser> _userManager;
        public float UserRatingStation {get; private set;}
        public GasStation? Station {get; private set;}
        public List<Comment>? Comments {get; private set;}
        public Info(IConfiguration configuration, UserManager<AppUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;

            _ctx = new Context(new Settings
            {
                UpdateDB = false,
                ConnectionDB = _configuration["Dbconnect"]!
            });

        }
        private void UpdateUser()
        {
            var userId = _userManager.GetUserId(User);
            if(userId is null)
                throw new ApplicationException("Non authorized user");

            var user = _userManager.FindByIdAsync(userId!).GetAwaiter().GetResult();
            string? uname = user?.UserName;
            if(uname is null)  
                throw new ApplicationException("Non authorized user");

            _user = DbApi.FindUserByName(_ctx, uname!);

            if(_user is null)
                throw new ApplicationException("Non authorized user");

        }

        public IActionResult OnGet()
        {
            UpdateUser();
            long id = -1;
            if(HttpContext.Request.Query["id"].Count >= 0 
                && long.TryParse(HttpContext.Request.Query["id"], out id))
            {
                Station = DbApi.GetStationById(_ctx, id);
                if(Station is null)
                    return RedirectToPage("Index");

                Comments = DbApi.GetComments(_ctx, Station);
            }
            else
            {
                return RedirectToPage("Index");
            }

            UserRatingStation = DbApi.GetUserStationRating(_ctx, _user!, Station!);
            
            return Page();
        }
        public DateTime? UpdatePetrol(Petrol petrol) => DbApi.GetUpdate(Station!, petrol);
        public (float rating, int stars) GetPetrolRating(Petrol petrol) => DbApi.GetPetrolRating(Station!, petrol);
        public float GetUserPetrolRating (Petrol petrol) => DbApi.GetUserPetrolRating(_ctx, _user!, Station!, petrol);

        public async Task<IActionResult> OnPost(long stationId, string fuelId, bool isStation, int rating)
        {
            UpdateUser();
            Station = DbApi.GetStationById(_ctx, stationId);

            if(!isStation){
                var fuel = Station?.Petrols?.Where(e => e.Name == fuelId).ToList();
                if(fuel?.Count > 0)
                    DbApi.SetPetrolStars(_ctx, _user!, fuel.First(), Station!, rating);
            }
            else
            {
                DbApi.SetStationStars(_ctx, _user!, Station!, rating);
            }

            return Page(); 
        }

        public async Task<IActionResult> OnPostComment(string msg, long stationId)
        {
            UpdateUser();
            Station = DbApi.GetStationById(_ctx, stationId);
            DbApi.SetComment(_ctx, _user!, Station!, msg);
            Comments = DbApi.GetComments(_ctx, Station!);
            return Page();
        }

        public async Task<IActionResult> OnPostMightPetrol(string petrolName, double? price, long stationId, string isExist)
        {
            UpdateUser();
            Station = DbApi.GetStationById(_ctx, stationId);
            Comments = DbApi.GetComments(_ctx, Station!);
            DbApi.SetMightPetrol(_ctx, _user!, Station!, petrolName.ToLower(), price, isExist == "true");
            return Page();
        }

    }
}