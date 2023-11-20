using System.Globalization;
using System.Net.Sockets;
using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Attributes;

namespace PiBox.Plugins.Messaging.Kafka.Flow
{
    [ReadinessCheck("kafka")]
    public class KafkaHealthCheck : IHealthCheck
    {
        private readonly char[] _bootstrapServerSeparators = { ',', ';' };
        private readonly ClientConfig _clientConfig;
        private readonly ILogger<KafkaHealthCheck> _logger;

        public KafkaHealthCheck(ClientConfig clientConfig, ILogger<KafkaHealthCheck> logger)
        {
            _clientConfig = clientConfig;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            var servers = _clientConfig.BootstrapServers.Split(_bootstrapServerSeparators)
                .Select(x => x.Split(':'))
                .Select(x => new { Host = x[0], Port = Convert.ToInt32(x[1], CultureInfo.InvariantCulture) })
                .ToList();
            foreach (var server in servers)
            {
                try
                {
                    using var tcpClient = new TcpClient();
                    var result = tcpClient.BeginConnect(server.Host, server.Port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    if (success)
                    {
                        tcpClient.EndConnect(result);
                        return Task.FromResult(HealthCheckResult.Healthy("Kafka is available."));
                    }
                    _logger.LogDebug("Kafka cluster member {Server}:{Host} is unavailable", server.Host, server.Port);
                }
                catch (Exception e)
                {
                    _logger.LogDebug(e, "Kafka cluster member {Server}:{Host} is unavailable", server.Host, server.Port);
                }
            }

            return Task.FromResult(HealthCheckResult.Unhealthy($"Kafka is unavailable. All servers are down."));
        }
    }
}
