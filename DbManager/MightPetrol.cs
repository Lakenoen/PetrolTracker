using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DbManager
{
    public class MightPetrol
    {
        [Key]
        public long Id {get;set;}
        public double? Price {get;set;}
        public bool? IsExist {get;set;}

        public long? PetrolId {get;set;}
        public Petrol? Petrol {get;set;}
    }
}