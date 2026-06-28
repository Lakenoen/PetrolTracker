using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DbManager;

[Index("Username", IsUnique = true)]
[Index("Email", IsUnique = true)]
public class User
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string Salt {get;set;} = string.Empty;

    public string Role { get; set; } = "user";
    public List<GasStationPetrol> GasStationPetrols {get;set;} = new List<GasStationPetrol>();
    public List<UserPetrolRating> UserPetrolRatings { get; set; } = new List<UserPetrolRating>();

    public List<GasStation> GasStations {get;set;} = new List<GasStation>();
    public List<UserGasStation> UserGasStations {get;set;} = new List<UserGasStation>();
    public double Trust {get;set;} = 0.0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public double LiterPer100km { get;set; } = 7.0;
    public double DriveType { get;set; } = 0.5;
    public double[] PreferenceVector { get;set; } = new double[4];
}
