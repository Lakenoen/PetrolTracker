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
        var users = ctx.Users.Where(u => u.Username == name).ToList();
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
        if(user.UserPetrolRatings.Where(p => p.UserId == user.Id && p.GasStationPetrolId == s_p.Id).ToList().Count() != 0)
            return false;
        
        s_p.Stars += 1;
        s_p.Rating = (s_p.Rating + rating) / s_p.Stars;
        user.UserPetrolRatings.Add(new UserPetrolRating {User = user, GasStationPetrol = s_p});
        ctx.Save();
        return true;
    }

    public static bool SetStationStars(Context ctx, User user, GasStation station, int rating)
    {
        if(user.UserGasStations.Where(p => p.UserId == user.Id && p.GasStationId == station.Id).ToList().Count() != 0)
            return false;
        
        station.Stars += 1;
        station.Rating = (station.Rating + rating) / station.Stars;
        user.UserGasStations.Add(new UserGasStation {User = user, GasStation = station});
        ctx.Save();
        return true;
    }

}