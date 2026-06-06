using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbManager;

public class GasStationPetrol
{
    public long Id {get;set;}
    public long GasStationId { get; set; }
    public required GasStation GasStation { get; set; }

    public string PetrolName { get; set; } = string.Empty;
    public double PetrolPrice { get; set; } = double.MinValue;
    public required Petrol Petrol { get; set; }

    public List<User> Users {get;set;} = new List<User>();
    public List<UserPetrolRating> UserPetrolRatings { get; set; } = new List<UserPetrolRating>();

    public DateTime? Update { get; set; } = null;
    public float Rating { get; set; } = 0;
    public int Stars { get; set; } = 0;
}
