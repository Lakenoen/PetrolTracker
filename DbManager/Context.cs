using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace DbManager
{
    public class Context : DbContext
    {
        public Calculator Calculator { get; init; }
        public DbSet<Petrol> Petrols { get; set; }
        public DbSet<GasStation> GasStations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Comment> Comments { get;set; }
        public DbSet<MightPetrol> MightPetrols {get;set;}
        public Settings Settings { get; init; }
        public object Locker { get; init; } = new();
        public Context(Settings settings)
        {
            Calculator = new Calculator(this);
            this.Settings = settings;
            if(Settings.UpdateDB)
                Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(Settings.ConnectionDB);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<GasStationPetrol>()
                .HasKey(p => p.Id);
            
            modelBuilder
                .Entity<GasStationPetrol>()
                .HasIndex(p => new {p.GasStationId, p.PetrolId})
                .IsUnique();

            modelBuilder
                .Entity<GasStation>()
                .HasMany(p => p.Petrols)
                .WithMany(p => p.Stations)
                .UsingEntity<GasStationPetrol>(
                    j => j.HasOne(p => p.Petrol).WithMany(p => p.GasStationPetrols).HasForeignKey(p => p.PetrolId)
                    .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne(p => p.GasStation).WithMany(p => p.GasStationPetrols).HasForeignKey(p => p.GasStationId)
                    .OnDelete(DeleteBehavior.Cascade)
                );

            modelBuilder
                .Entity<User>()
                .HasMany(p => p.GasStationPetrols)
                .WithMany(p => p.Users)
                .UsingEntity<UserPetrolRating>(
                    j => j.HasOne(j => j.GasStationPetrol).WithMany(p => p.UserPetrolRatings).HasForeignKey(p => p.GasStationPetrolId)
                    .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne(j => j.User).WithMany(p => p.UserPetrolRatings).HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                );

            modelBuilder
                .Entity<User>()
                .HasMany(p => p.GasStations)
                .WithMany(p => p.Users)
                .UsingEntity<UserGasStation>(
                    j => j.HasOne(j => j.GasStation).WithMany(p => p.UserGasStations).HasForeignKey(p => p.GasStationId)
                    .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne(j => j.User).WithMany(p => p.UserGasStations).HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                );

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Station)
                .WithMany(ugs => ugs.Comments)
                .HasForeignKey(c => new { c.UserId, c.GasStationId });

            modelBuilder.Entity<Petrol>()
                .HasOne(p => p.MightStation)
                .WithMany(ugs => ugs.Petrols)
                .HasForeignKey(c => new { c.UserId, c.GasStationId });
        }

        public void Save()
        {
            lock (Locker)
            {
                this.SaveChanges();
            }
        }
    }
}