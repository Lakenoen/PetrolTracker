using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbManager
{
    [PrimaryKey(nameof(UserId), nameof(GasStationPetrolId))]
    public class UserPetrolRating
    {
        public long UserId {get;set;}
        public required User User {get;set;}
        public long GasStationPetrolId {get;set;}
        public required GasStationPetrol GasStationPetrol {get;set;}
        public DateTime CreationTime {get;set;} = DateTime.Now;
    }
}