using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PetrolTracker;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DbManager;

public static class Utils
{
    public static List<GasStation> GetGasTations(Filter? filter, long page, long size)
    {
        if (filter is null)
        {
            var pages = Context.Instance.GasStations.FromSqlRaw(string.Format("SELECT * FROM \"GasStations\" LIMIT {0} OFFSET {1}", size, page));
            return pages.Include(s => s.Petrols).ThenInclude(s => s.GasStationPetrols).ToList();
        }
        else
        {
            string where = DbManager.Utils.MakeSqlFromFilter(filter);
            var pages = Context.Instance.GasStations
                .FromSqlRaw(string.Format("SELECT * FROM \"GasStations\" {0} LIMIT {1} OFFSET {2}", where, size, page));
            return pages.Include(s => s.Petrols).ThenInclude(s => s.GasStationPetrols).ToList();
        }
    }

    public static List<Petrol> GetAllPetrols()
    {
        return Context.Instance.Petrols
            .GroupBy(el => el.Name)
            .Select(g => new Petrol { Name = g.Key })
            .OrderBy(e => e.Name)
            .ToList();
    }
    public static DateTime GetUpdate(GasStation station, Petrol petrol)
    {
        return station.GasStationPetrols.Where(p => p.Petrol == petrol).ToList().First().Update!.Value;
    }

    private static void MakeStringFromFilter(Filter filter, StringBuilder sb)
    {
        sb.Append("(");

        for (int i = 0; i <  filter.Filters.Count - 1; i++)
        {
            MakeStringFromFilter(filter.Filters[i], sb);
            sb.Append(" ").Append(filter.Filters[i].Gop).Append(" ");
        }

        if(filter.Filters.Count == 0)
        {
            FixFilterForDB(filter);
            if (filter.Op == "between")
            {
                string[] values = filter.Value.Split('\t');
                sb.Append($"{filter.Field} {filter.Op} {values[0]} AND {values[1]}");
            }
            else
            {
                sb.Append($"{filter.Field} {filter.Op} {filter.Value}");
            }
        }
        else
        {
            MakeStringFromFilter(filter.Filters[filter.Filters.Count - 1], sb);
        }

        sb.Append(")");
    }

    private static void FixFilterForDB(Filter filter)
    {
        switch (filter.Op.ToLower())
        {
            case "=":
            case "equal": filter.Op = "="; break;
            case "!=":
            case "not_equal": filter.Op = "!="; break;
            case ">":
            case "more": filter.Op = ">"; break;
            case "<":
            case "less": filter.Op = "<"; break;
            case "between": filter.Op = "between"; break;
            default: throw new ApplicationException("Unknown operation");
        }

        switch (filter.Gop.ToLower())
        {
            case "&":
            case "and": filter.Gop = "and"; break;
            case "|":
            case "or": filter.Gop = "or"; break;
            default: throw new ApplicationException("Unknown operation");
        }

        string[] table_field = filter.Field.Split('.');
        for (int i = 0; i < table_field.Length; i++)
        {
            if (table_field[i][0] != '\"')
                table_field[i] = $"\"{table_field[i]}\"";
        }
        filter.Field = string.Join('.', table_field);
    }
    public static string MakeSqlFromFilter(Filter filter)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("where ");
        MakeStringFromFilter(filter, sb);
        return sb.ToString();
    }

}
