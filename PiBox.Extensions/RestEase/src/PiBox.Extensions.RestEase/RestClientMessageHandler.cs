using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PiBox.Extensions.RestEase.Authentication;
using Polly;

namespace PiBox.Extensions.RestEase
{
    internal class RestClientMessageHandler : HttpClientHandler
    {
        private readonly AuthHandler _authHandler;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

        public RestClientMessageHandler(AuthenticationConfig authenticationConfig, IAsyncPolicy<HttpResponseMessage> retryPolicy)
        {
            _authHandler = new AuthHandler(authenticationConfig);
            _retryPolicy = retryPolicy ?? Policy.HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500 && (int)r.StatusCode < 600)
                .RetryAsync(2);
        }

        [ExcludeFromCodeCoverage(Justification = "RestEase only calls the SendAsync method")]
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken).GetAwaiter().GetResult();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }

        private async Task<HttpResponseMessage> Handle(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cleanupContext = false;
            var context = request.GetPolicyExecutionContext();
            if (context is null)
            {
                context = new Context();
                request.SetPolicyExecutionContext(context);
                cleanupContext = true;
            }

            await _authHandler.HandleAuth(request, cancellationToken);

            try
            {
                return await _retryPolicy
                    .ExecuteAsync((c, ct) => SendMessageAsync(request, ct), context, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (cleanupContext)
                {
                    request.SetPolicyExecutionContext(null);
                }
            }
        }

        private Task<HttpResponseMessage> SendMessageAsync(HttpRequestMessage requestMessage,
            CancellationToken cancellationToken)
        {
            return base.SendAsync(requestMessage, cancellationToken);
        }
    }
}
