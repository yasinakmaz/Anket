using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;

namespace Anket.Services
{
    public static class SqlServices
    {
        public static string? _sqlserver;
        public static string? _sqlusername;
        public static string? _sqlpassword;
        public static string? _sqldatabasename;
        public static string? _connectionString;

        public static async Task LoadDataAsync()
        {
            try
            {
                _sqlserver = await SecureStorage.GetAsync("SQLSERVER") ?? ".";
                _sqlusername = await SecureStorage.GetAsync("SQLUSERNAME") ?? "sa";
                _sqlpassword = await SecureStorage.GetAsync("SQLPASSWORD") ?? "123456a.A";
                _sqldatabasename = await SecureStorage.GetAsync("SQLDATABASENAME") ?? "ANKET";

                // Güvenli bağlantı parametreleri eklenmiş connection string
                _connectionString = $"Data Source={_sqlserver};Initial Catalog={_sqldatabasename};Persist Security Info=True;User ID={_sqlusername};Password={_sqlpassword};Trust Server Certificate=True;Connection Timeout=30;Max Pool Size=200;Application Name=AnketApp";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SQL Ayarları yüklenirken hata: {ex.Message}");
                // Varsayılan değerleri kullan
                _sqlserver = ".";
                _sqlusername = "sa";
                _sqlpassword = "123456a.A";
                _sqldatabasename = "ANKET";
                _connectionString = $"Data Source={_sqlserver};Initial Catalog={_sqldatabasename};Persist Security Info=True;User ID={_sqlusername};Password={_sqlpassword};Trust Server Certificate=True;Connection Timeout=30;Max Pool Size=200;Application Name=AnketApp";
            }
        }

        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    // Bağlantı string henüz oluşturulmadıysa
                    _connectionString = $"Data Source={_sqlserver ?? "."};Initial Catalog={_sqldatabasename ?? "ANKET"};Persist Security Info=True;User ID={_sqlusername ?? "sa"};Password={_sqlpassword ?? "123456a.A"};Trust Server Certificate=True;Connection Timeout=30;Max Pool Size=200;Application Name=AnketApp";
                }
                return _connectionString;
            }
        }

        // Özel bağlantı sorunları için test metodu
        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var context = new AnketSqlContext(ConnectionString))
                {
                    // Basit bir sorgu ile bağlantıyı test et
                    return await context.Database.CanConnectAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SQL bağlantı testi başarısız: {ex.Message}");
                return false;
            }
        }
    }
}