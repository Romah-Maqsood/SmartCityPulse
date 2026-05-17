using MongoDB.Driver;
using SmartCityPulse.Models;

namespace SmartCityPulse.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        // Existing Users collection (Citizen & Admin)
        public IMongoCollection<AppUser> Users => _database.GetCollection<AppUser>("Users");

        // New Operators collection (only Operator role, separate table)
        public IMongoCollection<AppUser> Operators => _database.GetCollection<AppUser>("Operators");

        // Incidents collection
        public IMongoCollection<Incident> Incidents => _database.GetCollection<Incident>("Incidents");
    }
}