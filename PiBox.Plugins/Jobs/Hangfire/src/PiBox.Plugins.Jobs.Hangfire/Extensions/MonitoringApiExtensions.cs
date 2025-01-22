using Hangfire.Storage;

namespace PiBox.Plugins.Jobs.Hangfire.Extensions
{
    public static class MonitoringApiExtensions
    {
        private const int PageSize = 500;
        public static IList<T> GetCompleteList<T>(this IMonitoringApi api, Func<IMonitoringApi, HangfirePageOptions, IEnumerable<T>> action)
        {
            var pageOpts = new HangfirePageOptions(0, PageSize);
            var result = new List<T>();
            while (true)
            {
                var partialResult = action(api, pageOpts).ToList();
                result.AddRange(partialResult);
                if (partialResult.Count < pageOpts.PageSize)
                    break;
                pageOpts = pageOpts.Next();
            }
            return result;
        }
    }

    public record HangfirePageOptions(int Offset, int PageSize)
    {
        public HangfirePageOptions Next() => this with { Offset = Offset + PageSize };
    }
}
