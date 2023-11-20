using System;
using Newtonsoft.Json;

namespace PiBox.Extensions.RestEase.Authentication
{
    internal class OAuth2Model
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_expires_in")]
        public int RefreshExpiresIn { get; set; }

        [JsonIgnore] public DateTime Created { get; set; } = DateTime.UtcNow.AddSeconds(-1);

        public bool ShouldCreateToken() => IsExpired() && IsRefreshExpired();
        public bool ShouldRefreshToken() => IsExpired() && !IsRefreshExpired();
        private bool IsExpired() => Created.AddSeconds(ExpiresIn) < DateTime.UtcNow;
        private bool IsRefreshExpired() => Created.AddSeconds(RefreshExpiresIn) < DateTime.UtcNow;
    }
}
