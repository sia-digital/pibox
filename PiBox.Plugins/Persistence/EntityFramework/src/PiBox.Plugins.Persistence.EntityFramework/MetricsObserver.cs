using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PiBox.Hosting.Abstractions.Metrics;

namespace PiBox.Plugins.Persistence.EntityFramework
{
    public class MetricsObserver : IObserver<KeyValuePair<string, object>>
    {
        private readonly Histogram<long> _commandDurationInSeconds = Metrics.CreateHistogram<long>("efcore_command_duration_seconds", "items", "description");

        public void OnCompleted()
        {
            // do nothing
        }

        public void OnError(Exception error)
        {
            // do nothing
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            // source https://github.com/alexvaluyskiy/prometheus-net-contrib/blob/master/src/prometheus-net.EntityFramework/Diagnostics/EntityFrameworkListenerHandler.cs
            switch (value.Key)
            {
                case "Microsoft.EntityFrameworkCore.Infrastructure.ContextInitialized":
                    IncrementMetric("efcore_dbcontext_created_total");
                    return;
                case "Microsoft.EntityFrameworkCore.Infrastructure.ContextDisposed":
                    IncrementMetric("efcore_dbcontext_disposed_total");
                    return;
                case "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpened":
                    IncrementMetric("efcore_connection_opened_total");
                    return;
                case "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosed":
                    IncrementMetric("efcore_connection_closed_total");
                    return;
                case "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionError":
                    IncrementMetric("efcore_connection_errors_total");
                    return;
                case "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted":
                    {
                        if (value.Value is CommandExecutedEventData commandExecuted)
                        {
                            _commandDurationInSeconds.Record((long)commandExecuted.Duration.TotalSeconds);
                        }
                    }
                    return;
                case "Microsoft.EntityFrameworkCore.Database.Command.CommandError":
                    IncrementMetric("efcore_command_errors_total", new KeyValuePair<string, object>("label", "command"));
                    return;
                case "Microsoft.EntityFrameworkCore.Database.Transaction.TransactionCommitted":
                    if (value.Value is TransactionEndEventData _)
                    {
                        IncrementMetric("efcore_transaction_committed_total");
                    }

                    return;
                case "Microsoft.EntityFrameworkCore.Database.Transaction.TransactionRolledBack":
                    if (value.Value is TransactionEndEventData _)
                    {
                        IncrementMetric("efcore_transaction_rollback_total");
                    }

                    return;
                case "Microsoft.EntityFrameworkCore.Database.Transaction.TransactionError":
                    if (value.Value is TransactionErrorEventData _)
                    {
                        IncrementMetric("efcore_command_errors_total", new KeyValuePair<string, object>("label", "transaction"));
                    }

                    return;
                case "Microsoft.EntityFrameworkCore.Query.QueryPossibleUnintendedUseOfEqualsWarning":
                    IncrementMetric("efcore_query_warnings_total",
                        new KeyValuePair<string, object>("label", "QueryPossibleUnintendedUseOfEqualsWarning"));
                    return;
                case "Microsoft.EntityFrameworkCore.Query.QueryPossibleExceptionWithAggregateOperatorWarning":
                    IncrementMetric("efcore_query_warnings_total",
                        new KeyValuePair<string, object>("label", "QueryPossibleExceptionWithAggregateOperatorWarning"));
                    return;
                case "Microsoft.EntityFrameworkCore.Query.ModelValidationKeyDefaultValueWarning":
                    IncrementMetric("efcore_query_warnings_total",
                        new KeyValuePair<string, object>("label", "ModelValidationKeyDefaultValueWarning"));
                    return;
                case "Microsoft.EntityFrameworkCore.Query.BoolWithDefaultWarning":
                    IncrementMetric("efcore_query_warnings_total", new KeyValuePair<string, object>("label", "BoolWithDefaultWarning"));
                    return;
            }
        }

        private static void IncrementMetric(string name, KeyValuePair<string, object>? tags = null)
        {
            var counter = Metrics.CreateCounter<long>(name, "items", "description");
            switch (tags)
            {
                case null:
                    counter.Add(1);
                    break;
                default:
                    counter.Add(1, tags: tags.Value);
                    break;
            }
        }
    }
}
