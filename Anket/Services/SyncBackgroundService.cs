using System.Diagnostics;

namespace Anket.Services
{
    public class SyncBackgroundService : IDisposable
    {
        private readonly IConnectivityService _connectivityService;
        private Timer? _syncTimer;
        private bool _isSyncing;
        private bool _disposed;
        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
        private const int SyncIntervalMinutes = 30;

        public SyncBackgroundService(IConnectivityService connectivityService)
        {
            _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));
            _connectivityService.ConnectivityChanged += ConnectivityService_ConnectivityChanged;
            
            StartSyncTimer();
        }

        private void StartSyncTimer()
        {
            _syncTimer = new Timer(SyncTimerCallback, null, 
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(SyncIntervalMinutes));
        }

        private async void ConnectivityService_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            if (e.IsConnected && !_isSyncing)
            {
                await SyncOfflineDataAsync();
            }
        }

        private async void SyncTimerCallback(object state)
        {
            if (_isSyncing || !_connectivityService.IsConnected)
                return;

            await SyncOfflineDataAsync();
        }

        private async Task SyncOfflineDataAsync()
        {
            if (_isSyncing || !await _syncLock.WaitAsync(0))
                return;

            _isSyncing = true;

            try
            {
                var dbService = await DatabaseServiceFactory.GetDatabaseServiceAsync();
                if (dbService != null)
                {
                    await dbService.SyncOfflineDataAsync();
                    Debug.WriteLine($"Arka plan senkronizasyonu tamamlandı: {DateTime.Now}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Arka plan senkronizasyonu hatası: {ex.Message}");
            }
            finally
            {
                _isSyncing = false;
                _syncLock.Release();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _syncTimer?.Dispose();
                _syncTimer = null;
                
                if (_connectivityService != null)
                {
                    _connectivityService.ConnectivityChanged -= ConnectivityService_ConnectivityChanged;
                }
                
                _syncLock.Dispose();
            }

            _disposed = true;
        }
    }
} 