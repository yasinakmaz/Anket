using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.Maui.Storage;

namespace Anket.Services
{
    public class AnketSqlContext : DbContext
    {
        public DbSet<AnketRecord> TBLANKET { get; set; }

        private readonly string _connectionString;

        public AnketSqlContext(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Debug.WriteLine($"AnketSqlContext oluşturuldu. Bağlantı: {connectionString}");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                try
                {
                    optionsBuilder.UseSqlServer(_connectionString, options =>
                    {
                        options.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        options.CommandTimeout(60);
                    });

                    optionsBuilder.EnableDetailedErrors(true);
                    optionsBuilder.LogTo(message => Debug.WriteLine(message), LogLevel.Warning);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SQL Server yapılandırma hatası: {ex.Message}");
                    throw;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnketRecord>()
                .ToTable("TBLANKET")
                .HasKey(a => new { a.AnketID, a.Date, a.DeviceID });

            base.OnModelCreating(modelBuilder);
        }
    }
}