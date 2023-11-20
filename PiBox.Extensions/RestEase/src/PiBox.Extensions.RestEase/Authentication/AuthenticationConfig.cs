namespace PiBox.Extensions.RestEase.Authentication
{
    public class AuthenticationConfig
    {
        public bool Enabled { get; set; }
        public string BaseUrl { get; set; }
        public string TokenUrlPath { get; set; }
        public GrantType GrantType { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
