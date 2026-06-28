using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

namespace DbManager;

public static class DbApi
{
    public static Utils.ProtectDict<string, string> TempPassStorage = new Utils.ProtectDict<string, string>();

    public static GasStation? GetStationById(Context ctx, long id)
    {
        var stations = ctx.GasStations
            .Where(s => s.Id == id)
            .Include(s => s.Petrols)
            .ThenInclude(s => s.GasStationPetrols)
            .ThenInclude(s => s.UserPetrolRatings)
            .ToList();

        if(stations.Count() > 0)
            return stations.First();

        return null;
    }

    public static List<GasStation> GetStations(Context ctx, Filter? filter, long page, long size)
    {
        List<GasStation> result = new List<GasStation>();

        string where = "";
        if (filter is not null)
            where = DbManager.Utils.MakeSqlFromFilter(filter);
        

        var ids = Utils.SqlDynamicExecute(ctx, string.Format(Utils.GasStationsRawSQL, where, size, page), reader =>
        {
            return reader.GetInt64(0);
        }).Distinct().ToList();

        foreach (long id in ids)
        {
            GasStation station = ctx.GasStations.Where(s => s.Id == id)
                .Include(p => p.Petrols).ThenInclude(e => e.GasStationPetrols).First();
            result.Add(station);
        }
        return result;
    }

    public static List<Petrol> GetAllPetrols(Context ctx)
    {
        return ctx.Petrols
            .GroupBy(el => el.Name)
            .Select(g => new Petrol { Name = g.Key })
            .OrderBy(e => e.Name)
            .ToList();
    }

    public static (double min, double max) getPetrolPriceRange(Context ctx, params string[] petrols)
    {
        try{
            if(petrols.Count() <= 0)
                return (ctx.Petrols.Min(p => p.Price), ctx.Petrols.Max(p => p.Price));

            double min = ctx.Petrols.Max(p => p.Price);
            double max = ctx.Petrols.Min(p => p.Price);
            foreach(var pertrol in petrols){
                double localMin = ctx.Petrols.Where(p => p.Name == pertrol).Min(p => p.Price);
                double localMax = ctx.Petrols.Where(p => p.Name == pertrol).Max(p => p.Price);
                if( localMin < min )
                    min = localMin;
                if( localMin < min )
                    max = localMax;
            }

            return(min, max);
        }
        catch
        {
            return (0,0);
        }
    }

    public static DateTime GetUpdate(GasStation station, Petrol petrol)
    {
        var links = station.GasStationPetrols.Where(p => p.Petrol == petrol).ToList();
        if(links.Count() <= 0)
            return DateTime.Now;

        return links.First().Update!.Value;
    }

    public static (float rating, int stars) GetPetrolRating(GasStation station, Petrol petrol)
    {
        var links = station.GasStationPetrols.Where(p => p.Petrol == petrol).ToList();
        if(links.Count() <= 0)
            return (0, 0);

        return (links.First().Rating, links.First().Stars);
    }

    public static User? FindUserByName(Context ctx, string name)
    {
        var users = ctx.Users.Include(u => u.UserGasStations).Where(u => u.Username == name).ToList();
        if(users.Count() == 0)
            return null;
        return users.First();
    }

    public static User? FindUserByEmail(Context ctx, string email)
    {
        var users = ctx.Users.Where(u => u.Email == email).ToList();
        if(users.Count() == 0)
            return null;
        return users.First();
    }

    public static string CreateHashPassword(User user, string password)
    {
        var salt = Utils.CreateSalt();
        user.Salt = Convert.ToBase64String(salt);
        user.PasswordHash = Utils.CreateHashPassword(salt, password);
        return user.PasswordHash;
    }

    public static bool CheckPassword(User user, string password)
    {
        byte[] salt = Convert.FromBase64String(user.Salt);

        var externalHash = Utils.CreateHashPassword(salt, password);

        return externalHash == user.PasswordHash;
    }

    public static void AddUser(Context ctx, User user)
    {
        ctx.Add(user);
        ctx.Save();
    }
    public static bool SetPetrolStars(Context ctx, User user, Petrol petrol, GasStation station, int rating)
    {
        var s_p = petrol.GasStationPetrols.Where(p => p.GasStationId == station.Id).ToList().First();
        var upr = user.UserPetrolRatings.Where(p => p.UserId == user.Id && p.GasStationPetrolId == s_p.Id).ToList();
        if(upr.Count() != 0)
        {
            upr.First().Rating = rating;
        }
        else
        {
            s_p.Stars += 1;
            user.UserPetrolRatings.Add(new UserPetrolRating {User = user, GasStationPetrol = s_p, Rating = rating});
        }

        Task.Run(async ()=> {
            await ctx.Calculator.CalcPetrolRating(s_p);
        });
        
        ctx.Save();
        return true;
    }

    public static bool SetStationStars(Context ctx, User user, GasStation station, int rating)
    {
        var us = user.UserGasStations.Where(p => p.UserId == user.Id && p.GasStationId == station.Id).ToList();
        if(us.Count() != 0)
        {
            us.First().Rating = rating;
        }
        else
        {
            station.Stars += 1;
            user.UserGasStations.Add(new UserGasStation {User = user, GasStation = station, CreationTimeRating = DateTime.UtcNow});
        }

        Task.Run(async ()=> {
            await ctx.Calculator.CalcStationRating(station);
        });
        
        ctx.Save();
        return true;
    }

    public static float GetUserStationRating(Context _ctx, User user, GasStation station)
    {
        var usr = _ctx.Users.Where(u => u.Id == user.Id).Include(e => e.UserGasStations).Include(e => e.GasStations).ToList().First();
        var res = usr.UserGasStations.Where(e => e.GasStation.Id == station.Id).ToList();
        if(res.Count == 0)
            return 0.0f;
        
        return res.First().Rating;
    }

    public static float GetUserPetrolRating(Context _ctx, User user, GasStation st, Petrol petrol)
    {
        var petrols = st.GasStationPetrols.Where(e => e.Id == petrol.Id).ToList();
        if(petrols.Count == 0)
            return 0.0f;
        
        var res = user.UserPetrolRatings.Where(e => e.GasStationPetrol.Id == petrols.First().Id).ToList();
        if(res.Count == 0)
            return 0.0f;
        
        return res.First().Rating;
    }

    public static void SetComment(Context ctx, User user, GasStation st, string text)
    {
        var ugs = user.UserGasStations.Where(p => p.UserId == user.Id && p.GasStationId == st.Id).ToList();
        Comment? comment = null;
        if(ugs.Count == 0)
        {
            UserGasStation? nugs = null;
            user.UserGasStations.Add(nugs = new UserGasStation {User = user, GasStation = st, CreationTimeRating = DateTime.UtcNow});
            ctx.Comments.Add(comment = new Comment{Text = text, Station = nugs});
            nugs.Comments.Add(comment);
            ctx.Save();
            return;
        }

        ctx.Comments.Add(comment = new Comment{Text = text, UserId = ugs.First().UserId, GasStationId = ugs.First().GasStationId});
        ctx.Save();
    }

    public static List<Comment> GetComments(Context ctx, GasStation st)
    {
        var station = ctx.GasStations.Where(s => s.Id == st.Id).Include(s => s.UserGasStations).ThenInclude(ugs => ugs.Comments).ToList();
        if(station.Count == 0)
            throw new ApplicationException("GasStation is not exist");

        List<Comment> res = new List<Comment>();

        foreach(var gs in station.First().UserGasStations)
        {
            foreach(var comment in gs.Comments)
            {
                res.Add(comment);
            }
        }

        return res;
    }


    const short WaitHours = 5;
    public static void SetMightPetrol(Context ctx, User user, GasStation st, string petrolName, double? petrolPrice, bool isExist)
    {

        var ugsl = user.UserGasStations.Where(p => p.UserId == user.Id && p.GasStationId == st.Id).ToList();
        UserGasStation? ugs = null;
        if(ugsl.Count == 0)
            user.UserGasStations.Add(ugs = new UserGasStation {User = user, GasStation = st, CreationTimePetrol = DateTime.UtcNow});
        else
            ugs = ugsl.First();

    // if( (DateTime.UtcNow - ugs.CreationTimePetrol).Hours < WaitHours )
    //     return; //TODO

        if(petrolPrice is null || !isExist)
        {
            foreach( var p in st.Petrols.Where(p => p.Name == petrolName).ToList())
            {
                p.MightPetrols.Add(new MightPetrol{ IsExist = isExist});
                ugs.Petrols.Add(p);
            }
            ctx.Save();
            return;
        }

        var petrols = st.Petrols.Where(p => p.Name == petrolName).ToList();
        if(petrols.Count() > 0)
        {
            petrols.First().MightPetrols.Add(new MightPetrol{ IsExist = isExist, Price = petrolPrice.Value});
            ugs.Petrols.Add(petrols.First());
            ctx.Save();
            return;
        }

        return;

    }

    public static List<Petrol> GetMightPetrols(Context ctx, GasStation st)
    {
        var station = ctx.GasStations.Where(s => s.Id == st.Id).Include(s => s.UserGasStations).ThenInclude(ugs => ugs.Petrols).ToList();
        if(station.Count == 0)
            throw new ApplicationException("GasStation is not exist");

        List<Petrol> res = new List<Petrol>();

        foreach(var gs in station.First().UserGasStations)
        {
            foreach(var petrol in gs.Petrols)
            {
                if(petrol is null) continue;
                res.Add(petrol);
            }
        }

        return res;
    }

}