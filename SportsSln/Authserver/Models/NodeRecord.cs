namespace AuthServer.Models
{
    public class NodeRecord
    {
        public string NodeId { get; set; } = string.Empty;

        public string NodeName { get; set; } = string.Empty;

        public string PublicKey { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string IPAddress { get; set; } = string.Empty;

        public string SJC { get; set; } = string.Empty;

        public DateTime RegisteredAt { get; set; }

        public DateTime ExpiryDate { get; set; }
    }
}