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
            await SqlServices.LoadDataAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

            if (window != null)
            {
                window.Created += (s, e) => {
                };
            }

            return window;
        }
    }
}