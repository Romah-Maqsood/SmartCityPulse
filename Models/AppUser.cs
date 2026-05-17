using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartCityPulse.Models
{
    public class AppUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; } = "Citizen";      // Admin, Operator, Citizen
        public string Department { get; set; }              // Fire/Police/Rescue (for Operator)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}