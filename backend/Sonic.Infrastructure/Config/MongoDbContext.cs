using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Sonic.Infrastructure;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("MongoDbSettings:ConnectionString");
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(configuration.GetValue<string>("MongoDbSettings:DatabaseName"));
    }

    public IMongoDatabase GetDatabase() => _database;

    public IMongoCollection<T> GetCollection<T>(string collectionName)
        => _database.GetCollection<T>(collectionName);
}
