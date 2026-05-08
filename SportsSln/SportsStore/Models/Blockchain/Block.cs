using System.Security.Cryptography;
using System.Text;

namespace SportsStore.Models.Blockchain
{
    public class Block
    {
        // ===== Core =====
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public string PreviousHash { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;

        // ===== Node Identity =====
        public string NodeId { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // ===== Trust =====
        public double TrustWeight { get; set; }

        // ===== Security =====
        public string PublicKey { get; set; } = string.Empty;

        // 🔥 SJC (Signed by Auth Server)
        public string DigitalSignature { get; set; } = string.Empty;

        // ===== Hash =====
        public string CalculateHash()
        {
            string rawData =
                $"{Index}" +
                $"{Timestamp:O}" +
                $"{PreviousHash}" +
                $"{NodeId}" +
                $"{NodeName}" +
                $"{IPAddress}" +
                $"{Address}" +
                $"{TrustWeight}" +
                $"{PublicKey}" +
                $"{DigitalSignature}";

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return Convert.ToHexString(bytes);
            }
        }

        // ===== Basic Validation =====
        public bool IsValidStructure()
        {
            return !string.IsNullOrEmpty(NodeId)
                && !string.IsNullOrEmpty(PublicKey)
                && !string.IsNullOrEmpty(DigitalSignature)
                && !string.IsNullOrEmpty(Hash);
        }

        // ===== 🔐 RSA Verification =====
        public bool VerifySignature()
        {
            try
            {
                // 🔥 SJC format: base64(data) | hex(signature)
                var parts = DigitalSignature.Split('|');
                if (parts.Length != 2) return false;

                byte[] dataBytes = Convert.FromBase64String(parts[0]);
                byte[] signatureBytes = Convert.FromHexString(parts[1]);

                using var rsa = RSA.Create();

                // 🔥 PublicKey لازم يكون PEM
                rsa.ImportFromPem(PublicKey);

                return rsa.VerifyData(
                    dataBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }
    }
}