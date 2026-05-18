using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DbManager
{
    public class Context : IdentityDbContext<AppUser, IdentityRole, string>
    {
        // Синглтон для DbManager/GasLoader (прямой доступ без DI)
        private static Context? _instance = null;
        public static Context Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Context();
                return _instance;
            }
        }

        // DbSets для заправок
        public DbSet<Petrol> Petrols { get; set; }
        public DbSet<GasStation> GasStations { get; set; }

        // Старая таблица пользователей (до Identity) — оставляем для совместимости
        public DbSet<User> LegacyUsers { get; set; }

        // Конструктор для синглтона — OnConfiguring подхватит строку подключения
        private Context() { }

        // Конструктор для DI (ASP.NET Core Identity использует его)
        public Context(DbContextOptions<Context> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Если опции уже настроены через DI — не перезаписываем
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseNpgsql(GlobalSettings.ConnectionDB);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Обязательно: настройка таблиц Identity (AspNetUsers, AspNetRoles и т.д.)
            base.OnModelCreating(modelBuilder);

            // Связь GasStation ↔ Petrol через GasStationPetrol
            modelBuilder
                .Entity<GasStation>()
                .HasMany(p => p.Petrols)
                .WithMany(p => p.Stations)
                .UsingEntity<GasStationPetrol>(
                    j => j.HasOne(p => p.Petrol).WithMany(p => p.GasStationPetrols).HasForeignKey(p => new { p.PetrolName, p.PetrolPrice }),
                    j => j.HasOne(p => p.GasStation).WithMany(p => p.GasStationPetrols).HasForeignKey(p => p.GasStationId)
                );
        }
    }
}