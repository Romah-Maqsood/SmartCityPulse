using MongoDB.Driver;
using SmartCityPulse.Models;

namespace SmartCityPulse.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("MongoDB:ConnectionString");
            var databaseName = configuration.GetValue<string>("MongoDB:DatabaseName");
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<Incident> Incidents => _database.GetCollection<Incident>("Incidents");
        public IMongoCollection<AppUser> Users => _database.GetCollection<AppUser>("Users");
    }
}