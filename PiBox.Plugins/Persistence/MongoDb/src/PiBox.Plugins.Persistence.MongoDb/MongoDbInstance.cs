using System.Globalization;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using PiBox.Hosting.Abstractions.Attributes;
using PiBox.Hosting.Abstractions.Metrics;

namespace PiBox.Plugins.Persistence.MongoDb
{
    [Configuration("mongodb")]
    public class MongoDbConfiguration
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public string Database { get; set; } = null!;
        public string AuthDatabase { get; set; } = null!;
        public string User { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class MongoDbInstance : IMongoDbInstance
    {

        public MongoClient Client { get; }
        public IMongoDatabase Database { get; }

        public MongoDbInstance(MongoDbConfiguration mongoDbConfig)
        {

            Client = CreateMongoClient(mongoDbConfig.Host, mongoDbConfig.Port, mongoDbConfig.AuthDatabase,
                mongoDbConfig.Database, mongoDbConfig.User, mongoDbConfig.Password);
            Database = Client.GetDatabase(mongoDbConfig.Database);
        }

        private MongoClient CreateMongoClient(string host, int port, string authDb, string db, string user,
            string password)
        {
            var settings = new MongoClientSettings
            {
                Server = new MongoServerAddress(host, port),
                ConnectTimeout = TimeSpan.FromSeconds(5),
                HeartbeatInterval = TimeSpan.FromSeconds(5),
                ClusterConfigurator = SubscribeToDriverEvents
            };

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password)) return new MongoClient(settings);
            var authDbToUse = string.IsNullOrEmpty(authDb) ? db : authDb;
            settings.Credential = MongoCredential.CreateCredential(authDbToUse, user, password);

            return new MongoClient(settings);
        }

        private void SubscribeToDriverEvents(ClusterBuilder clusterBuilder)
        {
            clusterBuilder.Subscribe<ConnectionOpenedEvent>(IncrementCounter);
            clusterBuilder.Subscribe<ConnectionClosedEvent>(IncrementCounter);
            clusterBuilder.Subscribe<ConnectionFailedEvent>(IncrementCounter);
            clusterBuilder.Subscribe<ConnectionOpeningFailedEvent>(IncrementCounter);

            clusterBuilder.Subscribe<ServerHeartbeatFailedEvent>(IncrementCounter);
            clusterBuilder.Subscribe<ServerHeartbeatStartedEvent>(IncrementCounter);
            clusterBuilder.Subscribe<ServerHeartbeatSucceededEvent>(IncrementCounter);

            clusterBuilder.Subscribe<CommandStartedEvent>(IncrementCounter);
            clusterBuilder.Subscribe<CommandSucceededEvent>(IncrementCounter);
            clusterBuilder.Subscribe<CommandFailedEvent>(IncrementCounter);

            clusterBuilder.Subscribe<ClusterSelectingServerFailedEvent>(IncrementCounter);
            clusterBuilder.Subscribe<ConnectionReceivingMessageFailedEvent>(IncrementCounter);
            clusterBuilder.Subscribe<ConnectionSendingMessagesFailedEvent>(IncrementCounter);
        }

        public IMongoCollection<T> GetCollectionFor<T>()
        {
            var collectionName = typeof(T).Name;
            return Database.GetCollection<T>(collectionName);
        }

        internal static void IncrementCounter<T>(T eventObject)
        {
            Metrics.CreateCounter<long>($"mongodb_driver_{ToSnakeCase(eventObject.GetType().Name)}_total", "calls", "").Add(1);
        }

        internal static string ToSnakeCase(string s)
        {
            return SnakeCase.Replace(s);
        }
    }

    public static partial class SnakeCase
    {
        [GeneratedRegex(@"[A-Z]", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
        private static partial Regex SnakeCaseRegex();

        public static string Replace(string content) => SnakeCaseRegex().Replace(content, "_$0").TrimStart('_').ToLower(CultureInfo.InvariantCulture);
    }
}
