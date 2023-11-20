using System.Collections.Concurrent;

namespace PiBox.Plugins.Persistence.InMemory
{
    public class InMemoryStore
    {
        private readonly ConcurrentDictionary<Type, object> _dataStore = new();

        public HashSet<T> GetStore<T>() => _dataStore.GetOrAdd(typeof(T), new HashSet<T>()) as HashSet<T> ?? throw new InvalidOperationException();
    }
}
