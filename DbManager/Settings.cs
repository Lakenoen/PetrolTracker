using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbManager;

public class Settings
{
    public string ConnectionDB {get;set;} = "Host=localhost;Username=admin;Password=pass;Database=PetrolTracker";
    public bool UpdateDB {get;set;} = true;
}
