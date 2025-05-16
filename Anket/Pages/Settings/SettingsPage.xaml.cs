namespace Anket.Pages.Settings;

public partial class SettingsPage : ContentPage
{
	private readonly IFileSaver _fileSaver;
	
	public SettingsPage(IFileSaver fileSaver = null)
	{
		InitializeComponent();
		_fileSaver = fileSaver;
		Load();
        LoadAnketBaslik();
		LoadSettings();
	}

	private async Task Load()
	{
		EntryAnketBaslik.IsPassword = true;
        EntryAnketBaslik.IsEnabled = false;
        RbSqlite.IsEnabled = false;
        RbFirebase.IsEnabled = false;
        RbMssql.IsEnabled = false;
        TxtSqlServer.IsEnabled = false;
        TxtSqlServer.IsPassword = true;
        TxtSqlUserName.IsEnabled = false;
        TxtSqlUserName.IsPassword = true;
		TxtSqlPassword.IsEnabled = false;
        TxtSqlPassword.IsPassword = true;
		TxtSqlDatabase.IsEnabled = false;
		TxtSqlDatabase.IsPassword = true;
		EntryTimerSure.IsEnabled = false;
		EntryTimerSure.IsPassword = true;
		EntryPassword.IsEnabled = false;
        EntryPassword.IsPassword = true;
		EntryTesekkurMetni.IsEnabled = false;
		EntryTesekkurMetni.IsPassword = true;
		ChkRapor.IsEnabled = false;
        BtnKaydet.IsEnabled = false;
    }

    private async void LoadAnketBaslik()
	{
		try
		{
			string savedTitle = await SecureStorage.GetAsync("AnketBaslik");
			if (!string.IsNullOrEmpty(savedTitle))
			{
				EntryAnketBaslik.Text = savedTitle;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Başlık yüklenirken hata oluştu: {ex.Message}");
			await DisplayAlert("Hata", "Anket başlığı yüklenirken bir hata oluştu.", "Tamam");
		}
	}

	private async void LoadSettings()
	{
		try
		{
			// Veritabanı türünü yükle
			string dbType = await SecureStorage.GetAsync("DatabaseType") ?? "SQLite";

			TxtSqlServer.Text = SqlServices._sqlserver;
			TxtSqlUserName.Text = SqlServices._sqlusername;
			TxtSqlPassword.Text = SqlServices._sqlpassword;
			TxtSqlDatabase.Text	= SqlServices._sqldatabasename;

            // RadioButton ayarla
            switch (dbType)
			{
				case "SQLite":
					RbSqlite.IsChecked = true;
					break;
				case "Firebase":
					RbFirebase.IsChecked = true;
					break;
				case "MSSQL":
					RbMssql.IsChecked = true;
					break;
				default:
					RbSqlite.IsChecked = true;
					break;
			}

            string Rapor = await SecureStorage.GetAsync("RAPOR") ?? "0";
			if(Rapor == "0")
			{
				ChkRapor.IsChecked = false;
			}
			else
			{
				ChkRapor.IsChecked = true;
			}

				// Timer süresini yükle
				string timerDuration = await SecureStorage.GetAsync("TimerDuration") ?? "3";
			EntryTimerSure.Text = timerDuration;
            
            // Şifre ve teşekkür metnini yükle
            string password = await SecureStorage.GetAsync("AdminPassword") ?? "1234";
            EntryPassword.Text = password;
            
            string tesekkurMetni = await SecureStorage.GetAsync("TesekkurMetni") ?? "Katılımınız için teşekkürler!";
            EntryTesekkurMetni.Text = tesekkurMetni;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Ayarlar yüklenirken hata oluştu: {ex.Message}");
		}
	}

	private async void BtnKaydet_Clicked(object sender, EventArgs e)
	{
		try
		{
			string anketBaslik = EntryAnketBaslik.Text?.Trim() ?? string.Empty;
			await SecureStorage.SetAsync("AnketBaslik", anketBaslik);

			await SecureStorage.SetAsync("SQLSERVER",TxtSqlServer.Text);
            await SecureStorage.SetAsync("SQLUSERNAME", TxtSqlUserName.Text);
            await SecureStorage.SetAsync("SQLPASSWORD", TxtSqlPassword.Text);
            await SecureStorage.SetAsync("SQLDATABASENAME", TxtSqlDatabase.Text);

            string dbType = "SQLite"; 
			if (RbFirebase.IsChecked)
				dbType = "Firebase";
			else if (RbMssql.IsChecked)
				dbType = "MSSQL";
				
			await SecureStorage.SetAsync("DatabaseType", dbType);

            if (ChkRapor.IsChecked)
            {
                await SecureStorage.SetAsync("RAPOR", "1");
            }
            else
            {
                await SecureStorage.SetAsync("RAPOR", "0");
            }

            string timerDuration = EntryTimerSure.Text?.Trim() ?? "3";
			if (!int.TryParse(timerDuration, out int timerValue) || timerValue < 0)
			{
				timerDuration = "3";
				EntryTimerSure.Text = timerDuration;
			}
			await SecureStorage.SetAsync("TimerDuration", timerDuration);
            
            string password = EntryPassword.Text?.Trim() ?? "1234";
            if (string.IsNullOrEmpty(password))
            {
                password = "1234";
                EntryPassword.Text = password;
            }
            await SecureStorage.SetAsync("AdminPassword", password);
            
            string tesekkurMetni = EntryTesekkurMetni.Text?.Trim() ?? "Katılımınız için teşekkürler!";
            if (string.IsNullOrEmpty(tesekkurMetni))
            {
                tesekkurMetni = "Katılımınız için teşekkürler!";
                EntryTesekkurMetni.Text = tesekkurMetni;
            }
            await SecureStorage.SetAsync("TesekkurMetni", tesekkurMetni);
			
			await DisplayAlert("Bilgi", "Ayarlar başarıyla kaydedildi.", "Tamam");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Ayarlar kaydedilirken hata oluştu: {ex.Message}");
			await DisplayAlert("Hata", "Ayarlar kaydedilirken bir hata oluştu.", "Tamam");
		}
	}

    private async void BtnRaporlar_Clicked(object sender, EventArgs e)
    {
		if (_fileSaver != null)
		{
			await Navigation.PushAsync(new ReportPage(_fileSaver));
		}
		else
		{
			var fileSaver = IPlatformApplication.Current.Services.GetService<IFileSaver>();
			if (fileSaver != null)
			{
				await Navigation.PushAsync(new ReportPage(fileSaver));
			}
			else
			{
				await DisplayAlert("Hata", "Dosya kaydetme servisi başlatılamadı.", "Tamam");
			}
		}
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
		string cevap = await DisplayPromptAsync("Sistem", "Parola Giriniz", "Tamam", "Vazgeç", maxLength: 8, keyboard: Keyboard.Numeric);
		if (cevap == null)
		{
			await DisplayAlert("Sistem", "Lütfen Boş Giriş Yapmayınız", "Tamam");
		}
		else
		{
			if(cevap == "14531071")
			{
                EntryAnketBaslik.IsPassword = false;
                EntryAnketBaslik.IsEnabled = true;
                RbSqlite.IsEnabled = true;
                RbFirebase.IsEnabled = true;
                RbMssql.IsEnabled = true;
                TxtSqlServer.IsEnabled = true;
                TxtSqlServer.IsPassword = false;
                TxtSqlUserName.IsEnabled = true;
                TxtSqlUserName.IsPassword = false;
                TxtSqlPassword.IsEnabled = true;
                TxtSqlPassword.IsPassword = false;
                TxtSqlDatabase.IsEnabled = true;
                TxtSqlDatabase.IsPassword = false;
                EntryTimerSure.IsEnabled = true;
                EntryTimerSure.IsPassword = false;
                EntryPassword.IsEnabled = true;
                EntryPassword.IsPassword = false;
                EntryTesekkurMetni.IsEnabled = true;
                EntryTesekkurMetni.IsPassword = false;
                ChkRapor.IsEnabled = true;
                BtnKaydet.IsEnabled = true;
            }
			else
			{
                await DisplayAlert("Sistem", "Şifre Yanlış", "Tamam");
            }
		}
    }
}