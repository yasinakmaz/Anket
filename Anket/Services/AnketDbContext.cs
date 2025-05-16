using Anket.Models;
using Microsoft.EntityFrameworkCore;

namespace Anket.Services
{
    public class AnketDbContext : DbContext
    {
        public DbSet<AnketRecord> AnketRecords { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "anket_efcore.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // AnketRecord entitysini konfigüre et
            modelBuilder.Entity<AnketRecord>()
                .HasKey(a => new { a.AnketID, a.Date, a.DeviceID }); // Composite primary key
            
            base.OnModelCreating(modelBuilder);
        }
    }
} 