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

            // Veritaban� dosyas�n� olu�tur
            var dbFolder = Path.GetDirectoryName(DatabasePath);
            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }

            // Veritaban�n�n ilk kez yap�land�r�ld���ndan emin olun
            if (!_initialized)
            {
                lock (_lock)
                {
                    if (!_initialized)
                    {
                        // Veritaban�n� olu�tur
                        Database.EnsureCreated();
                        _initialized = true;
                        Debug.WriteLine($"SQLite veritaban� olu�turuldu: {DatabasePath}");
                    }
                }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // SQLite yap�land�rmas� - sabit ba�lant� dizesi
            var connectionString = $"Data Source={DatabasePath}";
            optionsBuilder.UseSqlite(connectionString);

            // Geli�tirme modunda hata ay�klama
            optionsBuilder.EnableDetailedErrors(true);
            optionsBuilder.EnableSensitiveDataLogging(true);
            optionsBuilder.LogTo(message => Debug.WriteLine($"EF CORE: {message}"), LogLevel.Error);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // AnketRecord model yap�land�rmas�
            modelBuilder.Entity<AnketRecord>(entity =>
            {
                // Tablo ad�n� a��k�a belirt
                entity.ToTable("AnketRecords");

                // Tekil bir birincil anahtar (composite anahtar yerine)
                entity.HasKey(e => e.Id);

                // Zorunlu alanlar� belirt
                entity.Property(e => e.AnketID).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.DeviceID).IsRequired();

                // �ndeksler (performans i�in)
                entity.HasIndex(e => new { e.AnketID, e.Date });
                entity.HasIndex(e => e.DeviceID);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}