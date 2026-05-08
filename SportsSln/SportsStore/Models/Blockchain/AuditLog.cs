namespace SportsStore.Models.Blockchain
{
    public class AuditLog
    {
        public string UserSession { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string BlockHash { get; set; }
    }

}
