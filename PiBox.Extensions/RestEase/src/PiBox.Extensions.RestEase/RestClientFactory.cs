using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PiBox.Extensions.RestEase.Authentication;
using Polly;
using RestEase;

namespace PiBox.Extensions.RestEase
{
    public static class RestClientFactory
    {
        public static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new()
        {
            Converters = new List<JsonConverter> { new StringEnumConverter() }
        };

        public static T Create<T>(string baseAddress) => Create<T>(baseAddress, null, null);
        public static T Create<T>(string baseAddress, AuthenticationConfig authenticationConfig) => Create<T>(baseAddress, authenticationConfig, null);
        public static T Create<T>(string baseAddress, IAsyncPolicy<HttpResponseMessage> retryPolicy) => Create<T>(baseAddress, null, retryPolicy);

        public static T Create<T>(string baseAddress, AuthenticationConfig authenticationConfig, IAsyncPolicy<HttpResponseMessage> retryPolicy) =>
            Create<T>(baseAddress, authenticationConfig, retryPolicy, DefaultJsonSerializerSettings);

        public static T Create<T>(string baseAddress, AuthenticationConfig authenticationConfig, IAsyncPolicy<HttpResponseMessage> retryPolicy, JsonSerializerSettings jsonSerializerSettings)
        {
            authenticationConfig ??= new AuthenticationConfig { Enabled = false };
            var restClientMessageHandler = new RestClientMessageHandler(authenticationConfig, retryPolicy);
            var restClient = new RestClient(baseAddress, restClientMessageHandler)
            {
                JsonSerializerSettings = jsonSerializerSettings
            };
            return restClient.For<T>();
        }
    }
}
