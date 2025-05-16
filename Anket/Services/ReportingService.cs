using Anket.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Anket.Services
{
    public class ReportingService
    {
        private readonly FirebaseAuthService _authService;
        private static readonly string FirebaseUrl = "https://anket-cb76d-default-rtdb.europe-west1.firebasedatabase.app/";
        private readonly IConnectivityService _connectivityService;
        private FirebaseClient? _firebaseClient;
        
        public ReportingService()
        {
            _authService = new FirebaseAuthService();
            _connectivityService = new ConnectivityService();
        }
        
        // Firebase istemcisine erişim
        private async Task<FirebaseClient> GetFirebaseClientAsync()
        {
            var authToken = await _authService.GetAuthTokenAsync();
            
            if (string.IsNullOrEmpty(authToken))
            {
                throw new InvalidOperationException("Firebase kimlik doğrulama token'ı alınamadı");
            }
            
            if (_firebaseClient == null)
            {
                var options = new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                };
                
                _firebaseClient = new FirebaseClient(FirebaseUrl, options);
            }
            
            return _firebaseClient;
        }
        
        /// <summary>
        /// Belirtilen tarih aralığındaki tüm anket kayıtlarını hem SQLite hem de Firebase'den çeker
        /// </summary>
        public async Task<ReportResults> GetReportDataAsync(DateTime startDate, DateTime endDate)
        {
            public async Task<ReportResults> GetReportDataAsync(DateTime startDate, DateTime endDate)
        {
            var results = new ReportResults();

            try
            {
                string answer = await SecureStorage.GetAsync("DatabaseType") ?? "SQLite";
                Debug.WriteLine($"Rapor için veritabanı tipi: {answer}");

                // DÜZELTME: VEYA (||) operatörünü kullanın
                if (answer == "SQLite" || answer == "Firebase")
                {
                    var sqliteData = await GetSqliteDataAsync(startDate, endDate);
                    results.SqliteRecords = sqliteData;

                    if (_connectivityService.IsConnected && answer == "Firebase")
                    {
                        var firebaseData = await GetFirebaseDataAsync(startDate, endDate);
                        results.FirebaseRecords = firebaseData;
                    }
                }
                else if (answer == "MSSQL")
                {
                    var mssqldata = await GetSqlDataAsync(startDate, endDate);
                    results.SqliteRecords = mssqldata;
                }
                else
                {
                    Debug.WriteLine($"Bilinmeyen veritabanı tipi: {answer}");
                }

                results.CalculateCounts();
                return results;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Rapor verileri getirilirken hata: {ex.Message}");
                throw;
            }
        }
        }
        
        /// <summary>
        /// SQLite veritabanından belirtilen tarih aralığındaki anket kayıtlarını çeker
        /// </summary>
        private async Task<List<AnketRecord>> GetSqliteDataAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var db = new AnketDbContext())
                {
                    // Tarih aralığına göre filtreleme yap
                    var records = await db.AnketRecords
                        .Where(r => r.Date >= startDate && r.Date <= endDate)
                        .OrderByDescending(r => r.Date)
                        .ToListAsync();
                    
                    return records;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SQLite verileri getirilirken hata: {ex.Message}");
                return new List<AnketRecord>();
            }
        }

        private async Task<List<AnketRecord>> GetSqlDataAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var db = new AnketSqlContext(SqlServices.ConnectionString))
                {
                    var records = await db.TBLANKET
                        .Where(r => r.Date >= startDate && r.Date <= endDate)
                        .OrderByDescending(r => r.Date)
                        .ToListAsync();

                    return records;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SQL verileri getirilirken hata: {ex.Message}");
                return new List<AnketRecord>();
            }
        }

        /// <summary>
        /// Firebase veritabanından belirtilen tarih aralığındaki anket kayıtlarını çeker
        /// </summary>
        private async Task<List<FirebaseAnketData>> GetFirebaseDataAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (!_connectivityService.IsConnected)
                {
                    return new List<FirebaseAnketData>();
                }
                
                var firebase = await GetFirebaseClientAsync();
                
                // Tüm kayıtları al (Firebase sorgu sınırlamaları nedeniyle)
                var allRecords = await firebase
                    .Child("anketler")
                    .OnceAsync<FirebaseAnket>();
                
                // Tarih aralığına göre filtreleme yap
                var filteredRecords = allRecords
                    .Where(r => {
                        if (DateTime.TryParse(r.Object.CreateDate, out DateTime recordDate))
                        {
                            return recordDate >= startDate && recordDate <= endDate;
                        }
                        return false;
                    })
                    .Select(r => new FirebaseAnketData
                    {
                        Key = r.Key,
                        MemnunInd = r.Object.MemnunInd,
                        CreateDate = r.Object.CreateDate,
                        DeviceID = r.Object.DeviceID,
                        IsProcessed = r.Object.IsProcessed
                    })
                    .OrderByDescending(r => r.CreateDate)
                    .ToList();
                
                return filteredRecords;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Firebase verileri getirilirken hata: {ex.Message}");
                return new List<FirebaseAnketData>();
            }
        }
        
        public static (DateTime startDate, DateTime endDate) GetDateRange(ReportDateRange range)
        {
            DateTime now = DateTime.Now;
            DateTime startDate;
            DateTime endDate = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
            
            switch (range)
            {
                case ReportDateRange.Today:
                    startDate = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                    break;
                
                case ReportDateRange.ThisWeek:
                    int daysToSubtract = (int)now.DayOfWeek == 0 ? 6 : (int)now.DayOfWeek - 1;
                    startDate = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(-daysToSubtract);
                    endDate = startDate.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;
                
                case ReportDateRange.ThisMonth:
                    startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                    endDate = startDate.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;
                
                case ReportDateRange.ThisYear:
                    startDate = new DateTime(now.Year, 1, 1, 0, 0, 0);
                    endDate = new DateTime(now.Year, 12, 31, 23, 59, 59);
                    break;
                
                default:
                    startDate = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                    break;
            }
            
            return (startDate, endDate);
        }
    }
    
    public enum ReportDateRange
    {
        Today,
        ThisWeek,
        ThisMonth,
        ThisYear,
        Custom
    }
    
    public record ReportResults
    {
        public List<AnketRecord> SqliteRecords { get; set; } = new();
        public List<FirebaseAnketData> FirebaseRecords { get; set; } = new();
        
        public int MutluCount { get; private set; }
        public int NotrCount { get; private set; }
        public int UzgunCount { get; private set; }
        
        public void CalculateCounts()
        {
            // SQLite verilerini say
            foreach (var record in SqliteRecords)
            {
                switch (record.AnketID)
                {
                    case 1001: // Mutlu
                        MutluCount++;
                        break;
                    case 1002: // Nötr
                        NotrCount++;
                        break;
                    case 1003: // Üzgün
                        UzgunCount++;
                        break;
                }
            }
            
            // Firebase verilerini say (SQLite ile çift sayımı önle)
            // SQLite verisinde olmayan Firebase verileri
            var deviceDatePairs = new HashSet<string>();
            foreach (var record in SqliteRecords)
            {
                string key = $"{record.DeviceID}_{record.Date:yyyyMMddHHmmss}";
                deviceDatePairs.Add(key);
            }
            
            foreach (var record in FirebaseRecords)
            {
                // Tarih formatlama için parse et
                if (DateTime.TryParse(record.CreateDate, out DateTime createDate))
                {
                    string key = $"{record.DeviceID}_{createDate:yyyyMMddHHmmss}";
                    
                    // Eğer bu kayıt zaten SQLite'da varsa, saymayı atla
                    if (deviceDatePairs.Contains(key))
                        continue;
                    
                    switch (record.MemnunInd)
                    {
                        case "0": // Mutlu
                            MutluCount++;
                            break;
                        case "1": // Nötr
                            NotrCount++;
                            break;
                        case "2": // Üzgün
                            UzgunCount++;
                            break;
                    }
                }
            }
        }
    }
    
    public record FirebaseAnketData
    {
        public string Key { get; init; } = string.Empty;
        public string MemnunInd { get; init; } = string.Empty;
        public string CreateDate { get; init; } = string.Empty;
        public string DeviceID { get; init; } = string.Empty;
        public bool IsProcessed { get; init; }
    }
} 