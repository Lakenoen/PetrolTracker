using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbManager;

[Index("Name", "Price")]
[Index("Name")]
[Index("Price")]
public class Petrol
{
    [Key]
    public long Id { get;set; }
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public double Price { get;set; }
    public bool isExist {get;set;} = true;
    public List<GasStation> Stations { get; set; } = new List<GasStation>();
    public List<GasStationPetrol> GasStationPetrols { get; set; } = new List<GasStationPetrol>();
    public long? UserId {get;set;}
    public long? GasStationId {get;set;}
    public UserGasStation? MightStation {get;set;}
    public List<MightPetrol>MightPetrols {get;set;} = new List<MightPetrol>();

}