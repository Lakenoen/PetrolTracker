using System;

namespace DbManager;

public class StationInfo
{
    public GasStation Station {get;set;} = new GasStation();
    public List<Petrol> Petrols {get;set;} = new List<Petrol>();
    public List<GasStationPetrol> StationPetols {get;set;} = new List<GasStationPetrol>();
}
