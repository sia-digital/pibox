using MongoDB.Driver;

namespace PiBox.Plugins.Persistence.MongoDb
{
    public interface IMongoDbInstance
    {
        MongoClient Client { get; }
        IMongoCollection<T> GetCollectionFor<T>();
    }
}
