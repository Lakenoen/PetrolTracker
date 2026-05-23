using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DbManager;

public static class Utils
{
    public class SqlCommandException : ApplicationException
    {
        public string CommandText {get; init;}
        public SqlCommandException(string command) : base()
        {
            CommandText = command;
        }
    }

    internal static string GasStationsRawSQL = @"SELECT * FROM ""GasStations""
                left join ""GasStationPetrol"" on ""GasStations"".""Id"" = ""GasStationPetrol"".""GasStationId""
                left join ""Petrols"" on ""Petrols"".""Name"" = ""GasStationPetrol"".""PetrolName""
					and ""Petrols"".""Price"" = ""GasStationPetrol"".""PetrolPrice""
                {0} LIMIT {1} OFFSET {2}";

    internal static List<dynamic> SqlDynamicExecute(Context ctx, string query, Func<DbDataReader,object> factory)
    {
        try{
            using var command = ctx.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;
            ctx.Database.OpenConnection();

            var results = new List<dynamic>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
                results.Add(factory.Invoke(reader));
            return results;
        }
        catch
        {
            throw new SqlCommandException(query);
        }
    }

    internal static void MakeStringFromFilter(Filter filter, StringBuilder sb)
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

    internal static void FixFilterForDB(Filter filter)
    {
        if( (filter.Value.Count() != 0 || filter.Field.Count() != 0) && filter.Filters.Count() != 0)
            throw new FilterException("The filter must be either a container or a low-level filter");

        if( !((filter.Field.Count() == 0 || filter.Value.Count() != 0) && (filter.Value.Count() == 0 || filter.Field.Count() != 0)) )
            throw new FilterException("Field must be <=> Value");

        switch (filter.Op.ToLower())
        {
            case "between":
                {
                    if(!filter.Type.ToLower().Equals("num"))
                        throw new FilterException("Mismatch between operation and type");
                };break;
            case "like":
                {
                    if(!filter.Type.ToLower().Equals("str"))
                        throw new FilterException("Mismatch between operation and type");
                };break;
        }

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
    internal static string MakeSqlFromFilter(Filter filter)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("where ");
        MakeStringFromFilter(filter, sb);
        return sb.ToString();
    }

    internal static byte[] CreateSalt()
    {
        const int SaltLength = 32;
        byte[] salt = new byte[SaltLength];
        var rngRand = RandomNumberGenerator.Create();
        rngRand.GetBytes(salt);
        return salt;
    }
    internal static string CreateHashPassword(byte[] salt, string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
        Buffer.BlockCopy(salt, 0, saltedPassword, 0, salt.Length);
        Buffer.BlockCopy(passwordBytes, 0, saltedPassword, salt.Length, passwordBytes.Length);

        using var hash = SHA256.Create();

        return Convert.ToBase64String(hash.ComputeHash(saltedPassword));
    }

    public class ProtectDict<Key, Value> : IDictionary<Key, Value> where Key : notnull
    {
        private readonly Dictionary<Key,Value> _dict = new Dictionary<Key, Value>();
        private object locker = new();

        public ProtectDict()
        {
            
        }

        public Value this[Key key] { 
            get
            {
                lock(locker){
                    return _dict[key];
                }
            }
            set
            {
                lock(locker){
                    _dict[key] = value;
                }
            }
        }

        public ICollection<Key> Keys
        {
            get
            {
                lock(locker){
                    var list = new List<Key>();
                    foreach(var el in _dict.Keys)
                    {
                        list.Add(el);
                    }
                    return list;
                }
            }
        }

        public ICollection<Value> Values 
        {
            get
            {
                lock(locker){
                    var list = new List<Value>();
                    foreach(var el in _dict.Values)
                    {
                        list.Add(el);
                    }
                    return list;
                }
            }
        }

        public int Count
        {
            get
            {
                lock(locker){
                    return _dict.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public void Add(Key key, Value value)
        {
            lock(locker){
                _dict.Add(key,value);
            }
        }

        public void Add(KeyValuePair<Key, Value> item)
        {
            lock(locker){
                _dict.Add(item.Key,item.Value);
            }
        }

        public void Clear()
        {
            lock(locker){
                _dict.Clear();
            }
        }

        public bool Contains(KeyValuePair<Key, Value> item)
        {
            lock(locker){
                return _dict.Contains(item);
            }
        }

        public bool ContainsKey(Key key)
        {
            lock(locker){
                return _dict.ContainsKey(key);
            }
        }

        public void CopyTo(KeyValuePair<Key, Value>[] array, int arrayIndex)
        {
            lock (locker)
            {
                int i = arrayIndex;
                foreach(KeyValuePair<Key,Value> el in _dict)
                {
                    if(i >= array.Length)
                        break;
                    array[i] = el;
                    ++i;
                }
            }
        }

        public IEnumerator<KeyValuePair<Key, Value>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(Key key)
        {
            lock(locker){
                return _dict.Remove(key);
            }
        }

        public bool Remove(KeyValuePair<Key, Value> item)
        {
            lock(locker){
                _dict.Remove(item.Key);
            }
            return true;
        }

        public bool TryGetValue(Key key, [MaybeNullWhen(false)] out Value value)
        {
            lock(locker){
                return _dict.TryGetValue(key, out value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}