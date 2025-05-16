using Anket.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Anket.Services
{
    public class AnketDbContext : DbContext
    {
        private static readonly string DatabaseName = "anket_efcore.db";
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseName);

        public DbSet<AnketRecord> AnketRecords { get; set; }

        public AnketDbContext()
        {
            SQLitePCL.Batteries_V2.Init();

            // Veritabaný dosyasýný oluþtur
            var dbFolder = Path.GetDirectoryName(DatabasePath);
            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }

            // Veritabanýnýn ilk kez yapýlandýrýldýðýndan emin olun
            if (!_initialized)
            {
                lock (_lock)
                {
                    if (!_initialized)
                    {
                        // Veritabanýný oluþtur
                        Database.EnsureCreated();
                        _initialized = true;
                        Debug.WriteLine($"SQLite veritabaný oluþturuldu: {DatabasePath}");
                    }
                }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // SQLite yapýlandýrmasý - sabit baðlantý dizesi
            var connectionString = $"Data Source={DatabasePath}";
            optionsBuilder.UseSqlite(connectionString);

            // Geliþtirme modunda hata ayýklama
            optionsBuilder.EnableDetailedErrors(true);
            optionsBuilder.EnableSensitiveDataLogging(true);
            optionsBuilder.LogTo(message => Debug.WriteLine($"EF CORE: {message}"), LogLevel.Error);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // AnketRecord model yapýlandýrmasý
            modelBuilder.Entity<AnketRecord>(entity =>
            {
                // Tablo adýný açýkça belirt
                entity.ToTable("AnketRecords");

                // Tekil bir birincil anahtar (composite anahtar yerine)
                entity.HasKey(e => e.Id);

                // Zorunlu alanlarý belirt
                entity.Property(e => e.AnketID).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.DeviceID).IsRequired();

                // Ýndeksler (performans için)
                entity.HasIndex(e => new { e.AnketID, e.Date });
                entity.HasIndex(e => e.DeviceID);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}