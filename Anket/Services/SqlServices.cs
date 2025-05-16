using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.Maui.Storage;

namespace Anket.Services
{
    public static class SqlServices
    {
        // Sabit bağlantı bilgileri - basitlik için
        public static readonly string DefaultServer = ".";
        public static readonly string DefaultUsername = "sa";
        public static readonly string DefaultPassword = "123456a.A";
        public static readonly string DefaultDatabase = "ANKET";

        // Ayarlardan yüklenen değerler
        public static string _sqlserver = DefaultServer;
        public static string _sqlusername = DefaultUsername;
        public static string _sqlpassword = DefaultPassword;
        public static string _sqldatabasename = DefaultDatabase;

        // Bağlantı dizesi
        public static string ConnectionString =>
            $"Data Source={_sqlserver};Initial Catalog={_sqldatabasename};Persist Security Info=True;" +
            $"User ID={_sqlusername};Password={_sqlpassword};TrustServerCertificate=True;" +
            $"Connection Timeout=30;Max Pool Size=200;Application Name=AnketApp";

        public static async Task LoadDataAsync()
        {
            try
            {
                _sqlserver = await SecureStorage.GetAsync("SQLSERVER") ?? DefaultServer;
                _sqlusername = await SecureStorage.GetAsync("SQLUSERNAME") ?? DefaultUsername;
                _sqlpassword = await SecureStorage.GetAsync("SQLPASSWORD") ?? DefaultPassword;
                _sqldatabasename = await SecureStorage.GetAsync("SQLDATABASENAME") ?? DefaultDatabase;

                Debug.WriteLine($"SQL Ayarları yüklendi: Server={_sqlserver}, Database={_sqldatabasename}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SQL Ayarları yüklenirken hata: {ex.Message}");
                // Varsayılan değerleri kullan
                _sqlserver = DefaultServer;
                _sqlusername = DefaultUsername;
                _sqlpassword = DefaultPassword;
                _sqldatabasename = DefaultDatabase;
            }
        }
    }
}