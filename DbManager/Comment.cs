using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbManager
{
    public class Comment
    {
        [Key]
        public long Id {get;set;}
        public string Text {get;set;} = "";
        public DateTime CreationTime {get;set;} = DateTime.UtcNow;
        public long UserId {get;set;}
        public long GasStationId {get;set;}
        public UserGasStation? Station {get;set;}
    }
}