using Microsoft.EntityFrameworkCore;
using Firebase.Database;
using Firebase.Database.Query;
using System.Threading;
using DocumentFormat.OpenXml.Bibliography;

namespace Anket.Services
{
    public interface IDatabaseService
    {
        Task SaveVoteAsync(string vote);
        Task SyncOfflineDataAsync();
        Task SaveVoteSqlServer(string vote);
    }

    public class SqliteDatabaseService : IDatabaseService
    {
        private readonly IConnectivityService _connectivityService;

        public SqliteDatabaseService(IConnectivityService connectivityService)
        {
            _connectivityService = connectivityService;
            
            // Veritabanını oluştur
            using (var db = new AnketDbContext())
            {
                db.Database.EnsureCreated();
            }
        }

        public async Task SaveVoteAsync(string vote)
        {
            try
            {
                int anketId = vote switch
                {
                    "Mutlu" => 1001,
                    "Nötr" => 1002,
                    "Üzgün" => 1003,
                    _ => 1002 // Varsayılan
                };

                // Cihaz ID
                string deviceId = await GetDeviceIdAsync();

                // Şu anki zaman
                DateTime now = DateTime.Now;

                // OzelDurum = 0, SQLite seçildiğinde Firebase'e gönderme işlemi olmayacak
                int ozelDurum = 0;

                // SQLite'a kaydet (EF Core ile)
                using (var db = new AnketDbContext())
                {
                    var anketRecord = new AnketRecord(anketId, now, ozelDurum, deviceId);
                    db.AnketRecords.Add(anketRecord);
                    await db.SaveChangesAsync();
                }

                // Debug.WriteLine($"SQLite (EF Core): Oy kaydedildi: {vote}, OzelDurum: {ozelDurum}");
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"SQLite kayıt hatası: {ex.Message}");
            }
        }
        public async Task SaveVoteSqlServer(string vote)
        {
            try
            {
                int anketId = vote switch
                {
                    "Mutlu" => 1001,
                    "Nötr" => 1002,
                    "Üzgün" => 1003,
                    _ => 1002
                };

                string deviceId = await GetDeviceIdAsync();

                DateTime now = DateTime.Now;

                int ozelDurum = 0;

                using (var _context = new AnketSqlContext(SqlServices.ConnectionString))
                {
                    var anket = new AnketRecord(anketId, now, ozelDurum, deviceId);
                    await _context.TBLANKET.AddAsync(anket);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Sistem", $"Hata : {ex.Message}", "Tamam");
            }
        }

        public async Task SyncOfflineDataAsync()
        {
            try
            {
                if (!_connectivityService.IsConnected)
                {
                    return; // İnternet bağlantısı yoksa senkronizasyon yapma
                }

                // EF Core ile offline kayıtları al
                using (var db = new AnketDbContext())
                {
                    // OzelDurum = 1 olan tüm offline kayıtları al
                    var offlineRecords = await db.AnketRecords
                        .Where(x => x.OzelDurum == 1)
                        .ToListAsync();

                    if (offlineRecords.Count == 0)
                    {
                        return; // Senkronize edilecek kayıt yoksa çık
                    }

                    var firebaseService = new FirebaseDatabaseService();

                    foreach (var record in offlineRecords)
                    {
                        try
                        {
                            string vote = record.AnketID switch
                            {
                                1001 => "Mutlu",
                                1002 => "Nötr",
                                1003 => "Üzgün",
                                _ => "Nötr"
                            };

                            // Firebase'e yükle (orijinal tarihle)
                            await firebaseService.SaveVoteAsync(vote, record.Date);

                            // Kaydı OzelDurum = 0 olarak güncelle (senkronize edilmiş)
                            // "with" ifadesi kullanarak yeni bir nesne oluştur
                            var updatedRecord = new AnketRecord(
                                record.AnketID,
                                record.Date,
                                0, // OzelDurum = 0 (senkronize edilmiş)
                                record.DeviceID,
                                true // IsProcessed = true
                            );

                            // Varolan kaydı sil ve yenisini ekle (EF Core'da record ile update yapmak için)
                            db.AnketRecords.Remove(record);
                            db.AnketRecords.Add(updatedRecord);
                            await db.SaveChangesAsync();

                            // Debug.WriteLine($"Offline kayıt senkronize edildi: {record.Date}");
                        }
                        catch (Exception ex)
                        {
                            // Debug.WriteLine($"Kayıt senkronizasyon hatası: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"Senkronizasyon hatası: {ex.Message}");
            }
        }

        private async Task<string> GetDeviceIdAsync()
        {
            string deviceId = await SecureStorage.GetAsync("DeviceID");
            
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Guid.NewGuid().ToString();
                await SecureStorage.SetAsync("DeviceID", deviceId);
            }
            
            return deviceId;
        }
    }

    public record FirebaseAnket
    {
        public string? MemnunInd { get; init; }
        public string? CreateDate { get; init; }
        public string? DeviceID { get; init; }
        public bool IsProcessed { get; init; }

        public FirebaseAnket()
        {
            MemnunInd = "";
            CreateDate = "";
            DeviceID = "";
            IsProcessed = false;
        }
    }

    public class FirebaseDatabaseService : IDatabaseService
    {
        private static readonly string FirebaseUrl = "https://anket-cb76d-default-rtdb.europe-west1.firebasedatabase.app/";
        private readonly FirebaseAuthService _authService;
        private readonly IConnectivityService _connectivityService;
        private FirebaseClient? _firebaseClient;
        private readonly string _lastConnectionLossTimeKey = "LastConnectionLossTime";
        private static readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
        private bool _isSyncing;
        
        public FirebaseDatabaseService()
        {
            _authService = new FirebaseAuthService();
            _connectivityService = new ConnectivityService();
            
            // Bağlantı değişikliklerini dinle
            _connectivityService.ConnectivityChanged += ConnectivityChanged;
        }
        
        // Bağlantı değişikliği olayı
        private async void ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            if (e.IsConnected && !_isSyncing)
            {
                // İnternet geldi, otomatik senkronizasyon başlat
                // Debug.WriteLine("Internet bağlantısı geri geldi, senkronizasyon başlatılıyor...");
                await SyncOfflineDataAsync();
            }
            else if (!e.IsConnected)
            {
                // İnternet gitti, tarih kaydet
                var lossTime = DateTime.Now;
                await SecureStorage.SetAsync(_lastConnectionLossTimeKey, lossTime.ToString("yyyy-MM-ddTHH:mm:ss"));
                // Debug.WriteLine($"Internet bağlantısı kesildi, zaman kaydedildi: {lossTime}");
            }
        }
        
        // Firebase istemcisine erişim - her çağrıda güncel token'la istemci oluştur
        private async Task<FirebaseClient> GetFirebaseClientAsync()
        {
            // Token al
            var authToken = await _authService.GetAuthTokenAsync();
            
            if (string.IsNullOrEmpty(authToken))
            {
                throw new InvalidOperationException("Firebase kimlik doğrulama token'ı alınamadı");
            }
            
            // Yeni Firebase istemcisi oluştur (veya mevcut olanı döndür)
            if (_firebaseClient == null)
            {
                // JWT token'ı kullanarak FirebaseOptions oluştur
                var options = new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                };
                
                _firebaseClient = new FirebaseClient(FirebaseUrl, options);
            }
            
            return _firebaseClient;
        }
        
        public async Task SaveVoteAsync(string vote)
        {
            // Şu anki zamanla kaydet
            await SaveVoteAsync(vote, DateTime.Now);
        }
        
        public async Task SaveVoteAsync(string vote, DateTime date)
        {
            try
            {
                // İnternet bağlantısı kontrolü
                if (!_connectivityService.IsConnected)
                {
                    // Debug.WriteLine("İnternet bağlantısı yok, SQLite'a yedekleniyor...");
                    await SaveToSqliteAsync(vote, date, isOffline: true);
                    return;
                }
                
                try
                {
                    // Firebase istemcisini al
                    var firebase = await GetFirebaseClientAsync();
                    
                    // Cihaz ID'sini al
                    string deviceId = await GetDeviceIdAsync();
                    
                    // Oy değerini belirle
                    string memnunInd = vote switch
                    {
                        "Mutlu" => "0",
                        "Nötr" => "1",
                        "Üzgün" => "2", 
                        _ => "1" // Varsayılan olarak Nötr
                    };
                    
                    // Firebase için veri modeli oluştur
                    var firebaseData = new FirebaseAnket
                    {
                        MemnunInd = memnunInd,
                        CreateDate = date.ToString("yyyy-MM-ddTHH:mm:ss"),
                        DeviceID = deviceId,
                        IsProcessed = true // Direkt olarak işlenmiş olarak işaretle
                    };
                    
                    // Firebase'e veri ekle
                    await firebase
                        .Child("anketler")
                        .PostAsync(firebaseData);
                    
                    // Debug.WriteLine($"Firebase: Oy kaydedildi: {vote}");
                }
                catch (Exception ex)
                {
                    // Debug.WriteLine($"Firebase kayıt hatası: {ex.Message}");
                    
                    // Firebase'e kayıt yapılamazsa SQLite'a yedekle
                    await SaveToSqliteAsync(vote, date, isOffline: true);
                }
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"Oy kaydetme hatası: {ex.Message}");
            }
        }
        
        // SQLite'a yedekleme metodu
        private async Task SaveToSqliteAsync(string vote, DateTime date, bool isOffline)
        {
            try
            {
                // Cihaz ID'sini al
                string deviceId = await GetDeviceIdAsync();
                
                // Anket ID'sini belirle
                int anketId = vote switch
                {
                    "Mutlu" => 1001,
                    "Nötr" => 1002,
                    "Üzgün" => 1003,
                    _ => 1002 // Varsayılan olarak Nötr
                };
                
                using (var db = new AnketDbContext())
                {
                    // Çevrimdışı oy kaydı oluştur (OzelDurum = 1)
                    var record = new AnketRecord(
                        anketID: anketId,
                        date: date,
                        ozelDurum: isOffline ? 1 : 0, // Çevrimdışı durumu
                        deviceID: deviceId,
                        isProcessed: false // Henüz senkronize edilmedi
                    );
                    
                    db.AnketRecords.Add(record);
                    await db.SaveChangesAsync();
                    
                    // Debug.WriteLine($"Oy SQLite'a kaydedildi: OzelDurum={record.OzelDurum}, İşlenmedi={!record.IsProcessed}");
                }
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"SQLite kayıt hatası: {ex.Message}");
            }
        }

        public async Task SaveVoteSqlServer(string vote)
        {
            try
            {
                int anketId = vote switch
                {
                    "Mutlu" => 1001,
                    "Nötr" => 1002,
                    "Üzgün" => 1003,
                    _ => 1002
                };

                string deviceId = await GetDeviceIdAsync();

                DateTime now = DateTime.Now;

                int ozelDurum = 0;

                using (var _context = new AnketSqlContext(SqlServices.ConnectionString))
                {
                    var anket = new AnketRecord(anketId, now, ozelDurum, deviceId);
                    await _context.TBLANKET.AddAsync(anket);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Sistem", $"Hata : {ex.Message}", "Tamam");
            }
        }

        public async Task SyncOfflineDataAsync()
        {
            // Kilidi al, aynı anda sadece bir senkronizasyon işlemi çalışsın
            if (!await _syncLock.WaitAsync(0))
            {
                // Debug.WriteLine("Senkronizasyon zaten devam ediyor, yeni işlem başlatılmadı");
                return;
            }
            
            try
            {
                _isSyncing = true;
                // İnternet bağlantısı yoksa çık
                if (!_connectivityService.IsConnected)
                {
                    // Debug.WriteLine("İnternet bağlantısı olmadan senkronizasyon yapılamaz");
                    return;
                }
                
                // EF Core ile offline kayıtları al
                using (var db = new AnketDbContext())
                {
                    // OzelDurum = 1 (Çevrimdışı) olan ve henüz işlenmemiş kayıtları al
                    var offlineRecords = await db.AnketRecords
                        .Where(x => x.OzelDurum == 1 && !x.IsProcessed)
                        .ToListAsync();
                    
                    if (offlineRecords.Count == 0)
                    {
                        // Debug.WriteLine("Senkronize edilecek çevrimdışı kayıt bulunamadı");
                        return;
                    }
                    
                    // Debug.WriteLine($"Toplam {offlineRecords.Count} çevrimdışı kayıt senkronize ediliyor...");
                    
                    // Firebase istemcisini al
                    var firebase = await GetFirebaseClientAsync();
                    
                    foreach (var record in offlineRecords)
                    {
                        try
                        {
                            // Oy değerini belirle
                            string vote = record.AnketID switch
                            {
                                1001 => "Mutlu",
                                1002 => "Nötr",
                                1003 => "Üzgün",
                                _ => "Nötr"
                            };
                            
                            // Firebase için veri modeli oluştur
                            var firebaseData = new FirebaseAnket
                            {
                                MemnunInd = vote == "Mutlu" ? "0" : vote == "Nötr" ? "1" : "2",
                                CreateDate = record.Date.ToString("yyyy-MM-ddTHH:mm:ss"),
                                DeviceID = record.DeviceID,
                                IsProcessed = true // Direkt olarak işlenmiş olarak işaretle
                            };
                            
                            // Firebase'e veri ekle (orijinal tarihle)
                            await firebase
                                .Child("anketler")
                                .PostAsync(firebaseData);
                            
                            // Yeni kayıt oluştur - record immutable olduğundan direkt değiştiremiyoruz
                            var updatedRecord = new AnketRecord(
                                record.AnketID,
                                record.Date,
                                0, // Artık çevrimiçi (OzelDurum = 0)
                                record.DeviceID,
                                true // İşlenmiş olarak işaretle (IsProcessed = true)
                            );
                            
                            // Eski kaydı sil, yeni kaydı ekle 
                            db.AnketRecords.Remove(record);
                            db.AnketRecords.Add(updatedRecord);
                            await db.SaveChangesAsync();
                            
                            // Debug.WriteLine($"Çevrimdışı kayıt senkronize edildi: {record.Date}");
                        }
                        catch (Exception ex)
                        {
                            // Debug.WriteLine($"Kayıt senkronizasyon hatası: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"Senkronizasyon hatası: {ex.Message}");
            }
            finally
            {
                _isSyncing = false;
                _syncLock.Release();
            }
        }
        
        // Firebase'deki işlenmemiş kayıtları güncelle
        private async Task ProcessFirebaseRecordsAsync(FirebaseClient firebase)
        {
            try
            {
                // İşlenmemiş kayıtları al
                var records = await firebase
                    .Child("anketler")
                    .OrderBy("IsProcessed")
                    .EqualTo(false)
                    .OnceAsync<FirebaseAnket>();
                
                if (records == null || !records.Any())
                {
                    // Debug.WriteLine("İşlenecek Firebase kaydı bulunamadı");
                    return;
                }
                
                // Debug.WriteLine($"Firebase'de {records.Count()} işlenmemiş kayıt bulundu");
                
                foreach (var record in records)
                {
                    // İşlenmiş olarak güncelle
                    var updatedData = new FirebaseAnket
                    {
                        MemnunInd = record.Object.MemnunInd,
                        CreateDate = record.Object.CreateDate,
                        DeviceID = record.Object.DeviceID,
                        IsProcessed = true // İşlenmiş olarak işaretle
                    };
                    
                    await firebase
                        .Child("anketler")
                        .Child(record.Key)
                        .PutAsync(updatedData);
                    
                    // Debug.WriteLine($"Firebase kaydı işlendi: {record.Key}");
                }
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"Firebase kayıtları işlenirken hata: {ex.Message}");
            }
        }
        
        private async Task<string> GetDeviceIdAsync()
        {
            string deviceId = await SecureStorage.GetAsync("DeviceID");
            
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Guid.NewGuid().ToString();
                await SecureStorage.SetAsync("DeviceID", deviceId);
            }
            
            return deviceId;
        }
    }

    public interface IConnectivityService
    {
        bool IsConnected { get; }
        event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;
    }

    public class ConnectivityChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; }

        public ConnectivityChangedEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }
    }

    public class ConnectivityService : IConnectivityService
    {
        public bool IsConnected => Connectivity.NetworkAccess == NetworkAccess.Internet;

        public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;

        public ConnectivityService()
        {
            Connectivity.ConnectivityChanged += (sender, args) =>
            {
                ConnectivityChanged?.Invoke(this, new ConnectivityChangedEventArgs(IsConnected));
            };
        }
    }

    public class DatabaseServiceFactory
    {
        private static readonly Lazy<IConnectivityService> _connectivityService = 
            new Lazy<IConnectivityService>(() => new ConnectivityService());

        public static async Task<IDatabaseService> GetDatabaseServiceAsync()
        {
            var connectivityService = _connectivityService.Value;
            string dbType = await SecureStorage.GetAsync("DatabaseType") ?? "SQLite";

            // Firebase seçilmiş ama internet yoksa SQLite'a yönlendir
            if (dbType == "Firebase" && !connectivityService.IsConnected)
            {
                // Debug.WriteLine("Firebase seçildi fakat internet bağlantısı yok. SQLite kullanılıyor...");
                return new SqliteDatabaseService(connectivityService);
            }

            return dbType switch
            {
                "Firebase" => new FirebaseDatabaseService(),
                _ => new SqliteDatabaseService(connectivityService) // Varsayılan olarak SQLite
            };
        }
    }
} 