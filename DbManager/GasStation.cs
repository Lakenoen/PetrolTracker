using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbManager;
public class GasStation
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<Petrol> Petrols { get; set; } = new List<Petrol>();
}
