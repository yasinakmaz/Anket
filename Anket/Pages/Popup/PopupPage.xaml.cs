namespace Anket.Pages.Popup;

public partial class PopupPage : ContentPage
{
    private int _remainingTime;
    private readonly string _voteResult;
    private static readonly Lazy<IConnectivityService> _connectivityService =
        new Lazy<IConnectivityService>(() => new ConnectivityService());

    public PopupPage(string voteResult)
    {
        InitializeComponent();
        _voteResult = voteResult ?? throw new ArgumentNullException(nameof(voteResult));

        // Sayfa yüklendiğinde timer'ı başlat
        Loaded += PopupPage_Loaded;
    }

    private async void PopupPage_Loaded(object sender, EventArgs e)
    {
        try
        {
            // Teşekkür metnini ayarlardan al
            string tesekkurMetni = await SecureStorage.GetAsync("TesekkurMetni") ?? "Katılımınız için teşekkürler!";
            string dbtype = await SecureStorage.GetAsync("DatabaseType") ?? "SQLite";
            LblTesekkur.Text = tesekkurMetni;

            Debug.WriteLine($"PopupPage: Seçilen veritabanı tipi: {dbtype}");

            // DÜZELTME: VEYA (||) operatörünü kullanın
            if (dbtype == "SQLite" || dbtype == "Firebase")
            {
                try
                {
                    // Oy bilgisini veritabanına kaydet
                    var dbService = await DatabaseServiceFactory.GetDatabaseServiceAsync();
                    if (dbService != null)
                    {
                        await dbService.SaveVoteAsync(_voteResult);
                        Debug.WriteLine($"Oy kaydedildi: {_voteResult}");
                    }
                    else
                    {
                        Debug.WriteLine("Veritabanı servisi oluşturulamadı!");
                    }

                    // İnternet bağlantısı kontrolü
                    if (!_connectivityService.Value.IsConnected)
                    {
                        _connectivityService.Value.ConnectivityChanged += ConnectivityChanged_Handler;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Oy kaydetme hatası: {ex.Message}");
                    // Kullanıcıya hata gösterme
                }
            }
            else if (dbtype == "MSSQL")
            {
                try
                {
                    var dbservice = await DatabaseServiceFactory.GetDatabaseServiceAsync();
                    if (dbservice != null)
                    {
                        await dbservice.SaveVoteSqlServer(_voteResult);
                        Debug.WriteLine($"SQL Server'a oy kaydedildi: {_voteResult}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SQL Server oy kaydetme hatası: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"Tanımlanamayan veritabanı tipi: {dbtype}");
            }

            // Timer süresini ayarla ve başlat
            string timerDurationStr = await SecureStorage.GetAsync("TimerDuration") ?? "3";
            if (!int.TryParse(timerDurationStr, out int timerDuration) || timerDuration < 1)
            {
                timerDuration = 3; // Varsayılan
            }

            _remainingTime = timerDuration;
            StartCountdown();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PopupPage yüklenirken hata: {ex.Message}");
            // Sayfa işleme hatası
        }
    }

    private async void ConnectivityChanged_Handler(object sender, AnketConnectivityEventArgs e)
    {
        if (e.IsConnected)
        {
            // İnternet bağlantısı tekrar sağlandığında, offline kayıtları senkronize et
            try
            {
                Debug.WriteLine("Internet bağlantısı tespit edildi, senkronizasyon başlatılıyor...");
                var dbService = await DatabaseServiceFactory.GetDatabaseServiceAsync();
                if (dbService != null)
                {
                    await dbService.SyncOfflineDataAsync();
                    Debug.WriteLine("Senkronizasyon tamamlandı");

                    _connectivityService.Value.ConnectivityChanged -= ConnectivityChanged_Handler;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Bağlantı değişikliği olayında hata: {ex.Message}");
            }
        }
    }

    private async void StartCountdown()
    {
        while (_remainingTime > 0)
        {
            LblGeriSayim.Text = $"Bu sayfa {_remainingTime} saniye içinde kapanacak...";
            await Task.Delay(1000);
            _remainingTime--;
        }

        await Navigation.PopAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Sayfa kapanırken event handler'ı temizle
        if (_connectivityService.IsValueCreated)
        {
            _connectivityService.Value.ConnectivityChanged -= ConnectivityChanged_Handler;
            Debug.WriteLine("Bağlantı değişikliği dinleyicisi kaldırıldı");
        }
    }
}