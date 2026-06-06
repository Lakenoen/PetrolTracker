using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbManager
{
    [PrimaryKey(nameof(UserId),nameof(GasStationId))]
    public class UserGasStation
    {
        public long UserId {get;set;}
        public required User User {get;set;}
        public long GasStationId {get;set;}
        public required GasStation GasStation {get;set;}
        public DateTime CreationTime {get;set;} = DateTime.Now;
    }
}