using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartCityPulse.Models
{
    public class AppUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = "Citizen";      // Admin, Operator, Citizen
        public string Department { get; set; } = string.Empty; // Fire/Police/Rescue (for Operator)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}