using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Cache;
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
        public int Rating {get;set;} = 0;
        public DateTime CreationTimeRating {get;set;}
        public DateTime CreationTimePetrol {get;set;}
        public List<Comment> Comments {get;set;} =  new List<Comment>();
        public List<Petrol> Petrols {get;set;} = new List<Petrol>();
    }
}