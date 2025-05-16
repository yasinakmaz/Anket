using System.Diagnostics.CodeAnalysis;

namespace Anket
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public partial class App : Application
    {
        private readonly SyncBackgroundService _syncService;
        public App(SyncBackgroundService syncService)
        {
            InitializeComponent();
            _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
            MainPage = new AppShell();
        }

        protected async override void OnStart()
        {
            base.OnStart();

            try
            {
                // SQL hizmetleri ve bağlantı ayarlarını yükle
                await SqlServices.LoadDataAsync();

                // Veritabanı tipini kontrol et
                string dbType = await SecureStorage.GetAsync("DatabaseType") ?? "SQLite";
                Debug.WriteLine($"Uygulama başlangıcı: Veritabanı tipi = {dbType}");

                // SQLite veritabanı otomatik olarak AnketDbContext constructor'ında oluşturuluyor
                // SQL Server bağlantı kontrolü
                if (dbType == "MSSQL")
                {
                    try
                    {
                        // SQL Server testi yapılabilir
                        Debug.WriteLine("MSSQL seçildi, SQL Server yapılandırması kullanılacak");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"SQL Server testi hatası: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Uygulama başlangıcında hata: {ex.Message}");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

            if (window != null)
            {
                window.Created += (s, e) => {
                    // Pencere oluşturulduğunda yapılacak işlemler
                };
            }

            return window;
        }
    }
}