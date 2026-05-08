namespace AuthServer.Models
{
    public class VerifyRequest
    {
        public string NodeId { get; set; } = string.Empty;

        public string SJC { get; set; } = string.Empty;

        // ?? ÈíÇäÇÊ ãæŞÚÉ (ãËáÇğ timestamp)
        public string Data { get; set; } = string.Empty;

        // ?? ÇáÊæŞíÚ
        public string Signature { get; set; } = string.Empty;
    }
}