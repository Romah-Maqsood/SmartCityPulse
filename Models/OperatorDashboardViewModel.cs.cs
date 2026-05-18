namespace SmartCityPulse.Models
{
    public class OperatorDashboardViewModel
    {
        public string OperatorName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int NewToday { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int TotalIncidents { get; set; }
        public List<Incident> RecentIncidents { get; set; } = new();
    }
}