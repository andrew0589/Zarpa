using System.Text.Json;
using NavigationES.Shared.Dtos;

namespace NavigationES.Client.Services
{
    public class AuthService
    {
        private const string AuthKey = "AuthKey";
        public LoggedInUser? User { get; set; }
        public string? Token { get; set; }

        public void Signin(AuthResponseDto dto)
        {
            var serialized = JsonSerializer.Serialize(dto);

            Preferences.Default.Set(AuthKey, serialized);

            (User, Token) = dto;
        }

        public void Initialize()
        {
            if (Preferences.Default.ContainsKey(AuthKey))
            {
                var serialized = Preferences.Default.Get<string?>(AuthKey, null);
                if (string.IsNullOrWhiteSpace(serialized))
                {
                    Preferences.Default.Remove(AuthKey);
                }
                else
                {
                    var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(serialized)!;
                    if (authResponse != null)
                    {
                        User = authResponse.user;
                        Token = authResponse.Token;
                    }
                }
            }
        }

        public void Signout()
        {
            Preferences.Default.Remove(AuthKey);
            (User, Token) = (null, null);
        }
    }
}
