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

            // Veritabanı dosyasını oluştur
            var dbFolder = Path.GetDirectoryName(DatabasePath);
            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }

            // Veritabanının ilk kez yapılandırıldığından emin olun
            if (!_initialized)
            {
                lock (_lock)
                {
                    if (!_initialized)
                    {
                        // Veritabanını oluştur
                        Database.EnsureCreated();
                        _initialized = true;
                        Debug.WriteLine($"SQLite veritabanı oluşturuldu: {DatabasePath}");
                    }
                }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // SQLite yapılandırması - sabit bağlantı dizesi
            var connectionString = $"Data Source={DatabasePath}";
            optionsBuilder.UseSqlite(connectionString);

            // Geliştirme modunda hata ayıklama
            optionsBuilder.EnableDetailedErrors(true);
            optionsBuilder.EnableSensitiveDataLogging(true);
            optionsBuilder.LogTo(message => Debug.WriteLine($"EF CORE: {message}"), LogLevel.Error);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // AnketRecord model yapılandırması
            modelBuilder.Entity<AnketRecord>(entity =>
            {
                // Tablo adını açıkça belirt
                entity.ToTable("AnketRecords");

                // Tekil bir birincil anahtar (composite anahtar yerine)
                entity.HasKey(e => e.Id);

                // Zorunlu alanları belirt
                entity.Property(e => e.AnketID).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.DeviceID).IsRequired();

                // İndeksler (performans için)
                entity.HasIndex(e => new { e.AnketID, e.Date });
                entity.HasIndex(e => e.DeviceID);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}