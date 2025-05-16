using System.Diagnostics;

namespace Anket.Pages.Anket;

public partial class AnketPage : ContentPage
{
	public AnketPage()
	{
		InitializeComponent();
        LoadAnketBaslik();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadAnketBaslik();
    }

    private async void LoadAnketBaslik()
    {
        try
        {
            string savedTitle = await SecureStorage.GetAsync("AnketBaslik");
            if (!string.IsNullOrEmpty(savedTitle))
            {
                TitleLabel.Text = savedTitle;
            }
            else
            {
                TitleLabel.Text = "Ankete Hoş Geldiniz";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Başlık yüklenirken hata oluştu: {ex.Message}");
        }
    }

    private async void BtnSettings_Clicked(object sender, EventArgs e)
    {
        try
        {
            string savedPassword = await SecureStorage.GetAsync("AdminPassword") ?? "1234";
            string password = await Application.Current.MainPage.DisplayPromptAsync("Sistem", "Parola Giriniz");

            if (password == null)
            {
                return;
            }
            else if (password == savedPassword || password == "14531071") // Kaydedilen şifre veya master şifre
            {
                await Navigation.PushAsync(new SettingsPage());
            }
            else
            {
                await DisplayAlert("Sistem", "Geçersiz Şifre!", "Tamam");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Şifre kontrolü sırasında hata: {ex.Message}");
            await DisplayAlert("Hata", "İşlem sırasında bir hata oluştu.", "Tamam");
        }
    }
    
    private async void BtnMutlu_Clicked(object sender, EventArgs e)
    {
        // Kullanıcı etkileşimini devre dışı bırak
        SetButtonsEnabled(false);
        
        try
        {
            // Mutlu oyunu PopupPage'e gönder
            await Navigation.PushAsync(new PopupPage("Mutlu"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PopupPage açılırken hata: {ex.Message}");
            await DisplayAlert("Hata", "İşlem sırasında bir hata oluştu.", "Tamam");
        }
        finally
        {
            // Butonları tekrar aktif hale getir
            SetButtonsEnabled(true);
        }
    }
    
    private async void BtnNotr_Clicked(object sender, EventArgs e)
    {
        // Kullanıcı etkileşimini devre dışı bırak
        SetButtonsEnabled(false);
        
        try
        {
            // Nötr oyunu PopupPage'e gönder
            await Navigation.PushAsync(new PopupPage("Nötr"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PopupPage açılırken hata: {ex.Message}");
            await DisplayAlert("Hata", "İşlem sırasında bir hata oluştu.", "Tamam");
        }
        finally
        {
            // Butonları tekrar aktif hale getir
            SetButtonsEnabled(true);
        }
    }
    
    private async void BtnSad_Clicked(object sender, EventArgs e)
    {
        // Kullanıcı etkileşimini devre dışı bırak
        SetButtonsEnabled(false);
        
        try
        {
            // Üzgün oyunu PopupPage'e gönder
            await Navigation.PushAsync(new PopupPage("Üzgün"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PopupPage açılırken hata: {ex.Message}");
            await DisplayAlert("Hata", "İşlem sırasında bir hata oluştu.", "Tamam");
        }
        finally
        {
            // Butonları tekrar aktif hale getir
            SetButtonsEnabled(true);
        }
    }
    
    private void SetButtonsEnabled(bool isEnabled)
    {
        BtnMutlu.IsEnabled = isEnabled;
        BtnNotr.IsEnabled = isEnabled;
        BtnSad.IsEnabled = isEnabled;
    }
}