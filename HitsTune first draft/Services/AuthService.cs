using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HitsTune_first_draft.Services
{
    public class AuthService
    {
        private const string FirebaseApiKey = "AIzaSyAhVL7tghcExD_xbfk1s5vjIENhCENk2rA";
        private static readonly HttpClient _httpClient = new HttpClient();

        public static string CurrentUserId { get; private set; }
        public static string CurrentUserEmail { get; private set; }
        public static string CurrentPlayerName { get; set; }
        public static string IdToken { get; private set; }

        // הרשמת משתמש חדש
        public async Task<bool> RegisterAsync(string email, string password, string playerName)
        {
            return await AuthenticateAsync("signUp", email, password, playerName);
        }

        // התחברות משתמש קיים
        public async Task<bool> LoginAsync(string email, string password, string playerName)
        {
            return await AuthenticateAsync("signInWithPassword", email, password, playerName);
        }

        private async Task<bool> AuthenticateAsync(string endpoint, string email, string password, string playerName)
        {
            try
            {
                var url = $"https://identitytoolkit.googleapis.com/v1/accounts:{endpoint}?key={FirebaseApiKey}";

                var payload = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AuthResponse>(responseJson);

                if (result != null && !string.IsNullOrEmpty(result.IdToken))
                {
                    IdToken = result.IdToken;
                    CurrentUserId = result.LocalId;
                    CurrentUserEmail = result.Email;
                    CurrentPlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auth Error ({endpoint}): {ex.Message}");
                return false;
            }
        }

        // התנתקות
        public void Logout()
        {
            IdToken = null;
            CurrentUserId = null;
            CurrentUserEmail = null;
            CurrentPlayerName = null;
        }

        private class AuthResponse
        {
            [JsonPropertyName("idToken")]
            public string IdToken { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }

            [JsonPropertyName("localId")]
            public string LocalId { get; set; }
        }
    }
}