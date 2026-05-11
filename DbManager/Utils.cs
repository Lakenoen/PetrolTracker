using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PetrolTracker;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DbManager;

public static class Utils
{
    private static string GasStationsRawSQL = @"SELECT * FROM ""GasStations""
                left join ""GasStationPetrol"" on ""GasStations"".""Id"" = ""GasStationPetrol"".""GasStationId""
                left join ""Petrols"" on ""Petrols"".""Name"" = ""GasStationPetrol"".""PetrolName""
					and ""Petrols"".""Price"" = ""GasStationPetrol"".""PetrolPrice""
                {0} LIMIT {1} OFFSET {2}";

    public static List<StationInfo> GetStationsInfo(Filter? filter, long page, long size)
    {
        var result = new Dictionary<long, StationInfo>();
        
        string where = "";
        if (filter is not null)
            where = DbManager.Utils.MakeSqlFromFilter(filter);
        

        SqlDynamicExecute(string.Format(GasStationsRawSQL, where, size, page), reader =>
        {
            if (result.TryGetValue(reader.GetInt64(0), out StationInfo? info))
            {
                info.Petrols.Add(new Petrol{
                    Name = reader.GetString(13),
                    Price = reader.GetDouble(14)
                    });
                info.StationPetols.Add(new GasStationPetrol
                {
                    GasStationId = reader.GetInt64(0),
                    PetrolName = reader.GetString(8),
                    PetrolPrice = reader.GetDouble(9),
                });
            }
            else
            {
                result[reader.GetInt64(0)] = new StationInfo{Station = new GasStation
                    {
                        Id = reader.GetInt64(0),
                    }
                };
            }
            return result[reader.GetInt64(0)];
        });

        foreach (StationInfo elem in result.Values)
        {
            elem.Station = Context.Instance.GasStations.Where(s => s.Id == elem.Station.Id)
                .Include(p => p.Petrols).ThenInclude(e => e.GasStationPetrols).First();
            elem.Petrols = elem.Station.Petrols;
            elem.StationPetols = elem.Station.GasStationPetrols;
        }
        return result.Values.ToList();
    }

    private static List<dynamic> SqlDynamicExecute(string query, Func<DbDataReader,object> factory)
    {
        using (var command = Context.Instance.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = query;
            Context.Instance.Database.OpenConnection();
            var results = new List<dynamic>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                    results.Add(factory.Invoke(reader));
            }
            return results;
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
            sb.Append(" ").Append(filter.Gop).Append(" ");
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
            case "like":
                {
                    filter.Op = "ilike";
                    filter.Value = $"%{filter.Value}%";
                }; break;
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

        if(filter.Type.ToLower() == "str")
            filter.Value = $"\'{filter.Value}\'";
    }
    public static string MakeSqlFromFilter(Filter filter)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("where ");
        MakeStringFromFilter(filter, sb);
        return sb.ToString();
    }

}
