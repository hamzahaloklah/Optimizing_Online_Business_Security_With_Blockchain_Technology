namespace AuthServer.Models
{
    public class RegisterResponse
    {
        public string NodeId { get; set; } = string.Empty;

        public string SJC { get; set; } = string.Empty;

        public DateTime ExpiryDate { get; set; }

        public string ServerPublicKey { get; set; } = string.Empty;
    }
}