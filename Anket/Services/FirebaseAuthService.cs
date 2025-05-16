using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anket.Services
{
    public class FirebaseAuthRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public bool ReturnSecureToken { get; set; } = true;
    }

    public class FirebaseAuthResponse
    {
        public string? IdToken { get; set; }
        public string? Email { get; set; }
        public string? LocalId { get; set; }
        public string? RefreshToken { get; set; }
        public string? ExpiresIn { get; set; }
        public bool Registered { get; set; }
    }

    public class FirebaseAuthService
    {
        private const string ApiKey = "AIzaSyDKGol_20tZXS8hAAvjJlHZt_x5ZaRVCow";
        private const string SignInUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={0}";
        private const string Email = "teknik@ozfiliz.com.tr";
        private const string Password = "123456a.A";
        
        private static readonly Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => new HttpClient());
        private HttpClient HttpClient => _httpClient.Value;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // İlk başarılı kimlik doğrulama sonucu alınan token'ı önbelleğe alma
        private static string? _cachedToken = null;
        private static DateTime _tokenExpiryTime = DateTime.MinValue;

        public async Task<string?> GetAuthTokenAsync()
        {
            // Token geçerli ise önbellekten döndür
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiryTime)
            {
                return _cachedToken;
            }

            try
            {
                // Kimlik doğrulama isteği hazırla
                var authRequest = new FirebaseAuthRequest
                {
                    Email = Email,
                    Password = Password
                };

                // Kimlik doğrulama isteği gönder
                var response = await HttpClient.PostAsJsonAsync(
                    string.Format(SignInUrl, ApiKey), 
                    authRequest, 
                    _jsonOptions);

                // İsteği kontrol et
                response.EnsureSuccessStatusCode();

                // Yanıtı oku
                var authResponse = await response.Content.ReadFromJsonAsync<FirebaseAuthResponse>(_jsonOptions);
                
                if (authResponse != null && !string.IsNullOrEmpty(authResponse.IdToken))
                {
                    // Token'ı önbelleğe al
                    _cachedToken = authResponse.IdToken;
                    
                    // Sona erme süresini hesapla (güvenlik için biraz marj ekle)
                    if (int.TryParse(authResponse.ExpiresIn, out int expiresInSeconds))
                    {
                        _tokenExpiryTime = DateTime.UtcNow.AddSeconds(expiresInSeconds - 60); // 1 dakika marj
                    }
                    else
                    {
                        // Varsayılan olarak 1 saat
                        _tokenExpiryTime = DateTime.UtcNow.AddHours(1);
                    }
                    
                    // Debug.WriteLine("Firebase kimlik doğrulama başarılı");
                    return _cachedToken;
                }
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"Firebase kimlik doğrulama hatası: {ex.Message}");
            }

            return null;
        }
    }
} 