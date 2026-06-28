using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbManager
{
    public class Calculator
    {
        readonly Context _ctx;
        public Calculator(Context ctx)
        {
            this._ctx = ctx;
        }

        public async Task CalcPetrolRating(GasStationPetrol petrol)
        {
            foreach(var uspr in petrol.UserPetrolRatings.ToList())
            {
                petrol.Rating += uspr.Rating;
            }
            
            petrol.Rating /= petrol.Stars;
        }

        public async Task CalcStationRating(GasStation station)
        {
            foreach(var usr in station.UserGasStations.ToList())
            {
                station.Rating += usr.Rating;
            }

            station.Rating /= station.Stars;
        }

        private static bool IsRun = false;
        private static int Hours = 1000 * 60 * 60 * 2;
        public static async Task Proccess(string connection, CancellationTokenSource token)
        {
            if(IsRun)
                return;

            IsRun = true;

            Settings settings = new Settings
            {
                UpdateDB = false,
                ConnectionDB = connection
            };
            Context ctx = new Context(settings);

            await Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    var stations = ctx.GasStations
                        .Include(e => e.UserGasStations)
                        .ThenInclude(e => e.Petrols)
                        .ThenInclude(e => e.MightPetrols)
                        .ToList();

                    foreach( var station in stations)
                    {
                        Dictionary<string, CalcMightInfo> info = new Dictionary<string, CalcMightInfo>();

                        foreach( var ugs in station.UserGasStations )
                        {
                            User usr = ctx.Users.Where(u => u.Id == ugs.UserId).ToList().First();

                            foreach( var petrol in ugs.Petrols)
                            {
                                CalcMightInfo? cmi = null;
                                double additionalTrust = 0.1;
                                if(!info.ContainsKey(petrol.Name)){
                                    cmi = new CalcMightInfo{ Ugs = ugs };

                                    var d = (DateTime.UtcNow - ugs.CreationTimePetrol).Days;
                                    additionalTrust = (1.0 / ((DateTime.UtcNow - ugs.CreationTimePetrol).Days + 1.0) ) / 5.0;

                                    foreach(var mp in petrol.MightPetrols){
                                        if(mp.Price is not null)
                                            cmi.Prices.Add((mp.Price.Value, usr.Trust + additionalTrust));
                                        if(mp.IsExist is not null)  
                                            cmi.Exists.Add(mp.IsExist.Value ? 1.0 : 0.0);
                                    }

                                    info[petrol.Name] = cmi;
                                    cmi.Petrol = petrol;
                                    continue;
                                }

                                cmi = info[petrol.Name];

                                additionalTrust = (1.0 / ((DateTime.UtcNow - ugs.CreationTimePetrol).Days + 1.0) ) / 5.0;

                                foreach(var mp in petrol.MightPetrols){
                                    if(mp.Price is not null)
                                        cmi.Prices.Add((mp.Price.Value, usr.Trust + additionalTrust));
                                    if(mp.IsExist is not null)  
                                        cmi.Exists.Add(mp.IsExist.Value ? 1.0 : 0.0);
                                }

                                cmi.Petrol = petrol;
                            }
                        }

                        foreach( CalcMightInfo cmi in info.Values)
                        {
                            cmi.Prices.Sort( (x, y) => x.price.CompareTo( y.price ));
                            cmi.Avarage = cmi.Prices.Select(e => e.price).Average();

                            double threshold = cmi.Prices.Select(e => e.trust).Sum() / 2.0;
                            double midwSum = 0.0;
                            for(int i = 0; i < cmi.Prices.Count; ++i)
                            {
                                midwSum += cmi.Prices[i].trust;
                                if(midwSum >= threshold)
                                {
                                    cmi.MidWeightPrice = cmi.Prices[i].price;
                                    break;
                                }
                            }

                            midwSum = 0.0;
                            for(int i = 0; i < cmi.Exists.Count; ++i)
                            {
                                midwSum += cmi.Prices[i].trust;
                                if(midwSum >= threshold)
                                {
                                    cmi.MidWeightExist = cmi.Exists[i];
                                    break;
                                }
                            }

                                
                            double acc = 0.0;
                            foreach ( double price in cmi.Prices.Select(e => e.price))
                            {
                                acc += Math.Pow( (price - cmi.Avarage).Value, 2.0) / cmi.Prices.Count;
                            }
                            cmi.Deviation = Math.Sqrt(acc);


                        }

                        foreach( var kvp in info)
                        {
                            if(kvp.Value.Petrol is null || kvp.Value.MidWeightPrice is null)
                                continue;
                            
                            var mightRange = ctx.MightPetrols.Where(e => e.PetrolId == kvp.Value.Petrol.Id).ToList();
                            kvp.Value.Petrol.MightPetrols.Clear();
                            ctx.MightPetrols.RemoveRange(mightRange);

                            kvp.Value.Petrol.Price = kvp.Value.MidWeightPrice.Value;

                            if( kvp.Value.MidWeightExist is null )
                                continue;

                            kvp.Value.Petrol.isExist = kvp.Value.MidWeightExist.Value > 0.5 ? true : false;

                            ctx.Save();
                            continue;

                        }


                        foreach( CalcMightInfo cmi in info.Values)
                        {
                            if(cmi.Deviation is null || cmi.Ugs is null)
                                continue;

                            if(DateTime.UtcNow - cmi.Ugs.CreationTimePetrol > TimeSpan.FromDays(1))
                                continue;
                            
                            double q = cmi.Deviation.Value / (cmi.Prices.Select(e => e.price).Max() - cmi.Prices.Select(e => e.price).Min());

                            cmi.Ugs.User.Trust = Math.Pow(Math.E, -3.0 * q);
                            ctx.Save();
                        }

                    }
                    Task.Delay(Hours).Wait();
                }
            });
        }

        private class CalcMightInfo
        {
            public UserGasStation? Ugs {get;set;} = null;
            public Petrol? Petrol {get;set;} = null;
            public List<(double price, double trust)> Prices {get;set;} = new List<(double price, double trust)>();
            public List<double> Exists {get;set;} = new List<double>();
            public double? MidWeightPrice {get;set;} = null;
            public double? MidWeightExist {get;set;} = null;
            public double? Avarage {get;set;} = null;
            public double? Deviation {get;set;} = null;
        };
    }

}