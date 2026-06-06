using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbManager;

[Index("Name")]
[Index("Latitude", "Longitude")]
[Index("Name", "Latitude", "Longitude")]
public class GasStation
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    [Required]
    public double Latitude { get; set; } = double.MinValue;

    [Required]
    public double Longitude { get; set; } = double.MinValue;
    public float Rating { get; set; } = 0;
    public int Stars { get; set; } = 0;
    public List<Petrol> Petrols { get; set; } = new List<Petrol>();
    public List<GasStationPetrol> GasStationPetrols { get; set; } = new List<GasStationPetrol>();

    public List<User> Users {get;set;} = new List<User>();
    public List<UserGasStation> UserGasStations {get;set;} = new List<UserGasStation>();
}
