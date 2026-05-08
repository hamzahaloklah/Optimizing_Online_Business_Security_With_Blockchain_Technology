namespace AuthServer.Models
{
    public class RegisterRequest
    {
        // 🔐 المفتاح العام للعقدة
        public string PublicKey { get; set; } = string.Empty;

        // 🏷️ اسم العقدة
        public string NodeName { get; set; } = string.Empty;

        // 🌐 عنوان العقدة (URL)
        public string Address { get; set; } = string.Empty;
    }
}