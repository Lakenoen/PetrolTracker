using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbManager;

public static class Utils
{
    public static List<GasStation> GetGasTations(long page, long size)
    {
        var pages = Context.Instance.GasStations.FromSqlRaw(string.Format("SELECT * FROM \"GasStations\" gs LIMIT {0} OFFSET {1}", size, page));
        return pages.Include(s => s.Petrols).ThenInclude(s => s.GasStationPetrols).ToList();
    }

    public static DateTime GetUpdate(GasStation station, Petrol petrol)
    {
        return station.GasStationPetrols.Where(p => p.Petrol == petrol).ToList().First().Update!.Value;
    }
}
