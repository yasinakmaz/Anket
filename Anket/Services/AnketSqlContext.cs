using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;
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
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_connectionString, options =>
                {
                    // Geçici hatalar için yeniden deneme politikası ekle
                    options.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null
                    );

                    // Komut timeout süresini artır
                    options.CommandTimeout(60);

                    // EnableRetryOnFailure eklendiğinde gerekli
                    options.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                });
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