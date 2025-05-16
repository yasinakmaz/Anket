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
            
            if(dbtype == "SQLite" && dbtype == "Firebase")
            {
                // Oy bilgisini seçilen veritabanına kaydet
                var dbService = await DatabaseServiceFactory.GetDatabaseServiceAsync();
                if (dbService != null)
                {
                    await dbService.SaveVoteAsync(_voteResult);
                }

                // İnternet bağlantısı değişikliğini dinle
                bool isNotConnected = !_connectivityService.Value.IsConnected;
                if (isNotConnected)
                {
                    _connectivityService.Value.ConnectivityChanged += ConnectivityChanged_Handler;
                }

                // Timer süresini ayarlardan al
                string timerDurationStr = await SecureStorage.GetAsync("TimerDuration") ?? "3";
                if (!int.TryParse(timerDurationStr, out int timerDuration) || timerDuration < 1)
                {
                    timerDuration = 1; // Geçersiz değer veya 0 değeri için varsayılan 1 saniye
                }

                _remainingTime = timerDuration;
                StartCountdown();
            }
            else 
            {
                var dbservice = await DatabaseServiceFactory.GetDatabaseServiceAsync();
                if (dbservice != null)
                {
                    await dbservice.SaveVoteSqlServer(_voteResult);
                }
                // Timer süresini ayarlardan al
                string timerDurationStr = await SecureStorage.GetAsync("TimerDuration") ?? "3";
                if (!int.TryParse(timerDurationStr, out int timerDuration) || timerDuration < 1)
                {
                    timerDuration = 1; // Geçersiz değer veya 0 değeri için varsayılan 1 saniye
                }

                _remainingTime = timerDuration;
                StartCountdown();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PopupPage yüklenirken hata oluştu: {ex.Message}");
        }
    }
    
    private async void ConnectivityChanged_Handler(object sender, AnketConnectivityEventArgs e)
    {
        if (e.IsConnected)
        {
            // İnternet bağlantısı tekrar sağlandığında, offline kayıtları senkronize et
            try
            {
                var dbService = await DatabaseServiceFactory.GetDatabaseServiceAsync();
                if (dbService != null)
                {
                    await dbService.SyncOfflineDataAsync();
                    
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
        }
    }
}