using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartCityPulse.Models
{
    public class Incident
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("location")]
        public string Location { get; set; } = string.Empty;

        [BsonElement("latitude")]
        public double? Latitude { get; set; }

        [BsonElement("longitude")]
        public double? Longitude { get; set; }

        [BsonElement("severity")]
        public string Severity { get; set; } = "Medium";

        [BsonElement("department")]      // ✅ YEH PROPERTY ADD KARO
        public string Department { get; set; } = "Unassigned";

        [BsonElement("status")]
        public string Status { get; set; } = "Open";

        [BsonElement("reportedAt")]
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("comments")]
        public List<IncidentComment> Comments { get; set; } = new();
    }

    public class IncidentComment
    {
        public string Text { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}