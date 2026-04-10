using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DbManager
{
    public class Context : DbContext
    {
        private static Context? _instance = null;
        public static Context Instance
        {
            get
            {
                if( _instance == null )
                    _instance = new Context();
                return _instance;
            }
        }
        public DbSet<Petrol> Petrols { get; set; }
        public DbSet<GasStation> GasStations { get; set; }

        private Context()
        {
            if(GlobalSettings.UpdateDB)
                Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(GlobalSettings.ConnectionDB);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<GasStation>()
                .HasMany(p => p.Petrols)
                .WithMany(p => p.Stations)
                .UsingEntity<GasStationPetrol>(
                    j => j.HasOne(p => p.Petrol).WithMany(p => p.GasStationPetrols).HasForeignKey(p => new { p.PetrolName, p.PetrolPrice}),
                    j => j.HasOne(p => p.GasStation).WithMany(p => p.GasStationPetrols).HasForeignKey(p => p.GasStationId)
                );
        }
    }
}
