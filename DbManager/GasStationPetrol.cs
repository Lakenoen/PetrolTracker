using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbManager;

[PrimaryKey(nameof(GasStationId), nameof(PetrolName), nameof(PetrolPrice))]
public class GasStationPetrol
{
    public long GasStationId { get; set; }
    public GasStation? GasStation { get; set; }

    public string PetrolName { get; set; } = string.Empty;
    public double PetrolPrice { get; set; } = double.MinValue;
    public Petrol? Petrol { get; set; }

    public DateTime? Update { get; set; } = null;
}
