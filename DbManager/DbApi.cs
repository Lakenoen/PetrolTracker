using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DbManager;

public static class DbApi
{
    public static Utils.ProtectDict<string, string> TempPassStorage = new Utils.ProtectDict<string, string>();
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

    public static (double min, double max) getPetrolPriceRange(Context ctx)
    {
        return (ctx.Petrols.Min(p => p.Price), ctx.Petrols.Max(p => p.Price));
    }
    public static DateTime GetUpdate(GasStation station, Petrol petrol)
    {
        return station.GasStationPetrols.Where(p => p.Petrol == petrol).ToList().First().Update!.Value;
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
        lock (ctx.Locker)
        {
            ctx.SaveChanges();
        }
    }

}