using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbManager;

public static class GlobalSettings
{
    public static string ConnectionDB = "Host=localhost;Username=postgres;Password=pass;Database=PetrolTracker";
    public static bool UpdateDB = true;
}
