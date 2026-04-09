using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbManager;

[PrimaryKey(nameof(Name), nameof(Price))]
public class Petrol
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public double Price { get;set; }

    public DateTime? Update { get; set; } = null;

    public List<GasStation> Stations { get; set; } = new List<GasStation>();

}