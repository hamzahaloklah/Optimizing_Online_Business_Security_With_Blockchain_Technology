using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AuthServer.Services
{
    public class AuthService
    {
        private readonly string _privateKey;
        private readonly string _publicKey;

        public AuthService()
        {
            using var rsa = RSA.Create(2048);

            _privateKey = rsa.ExportPkcs8PrivateKeyPem();
            _publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        }

        // ================= GENERATE =================
        public (string NodeId, string SJC, DateTime Expiry) GenerateIdentity(string nodePublicKey)
        {
            string nodeId = "Node-" + Guid.NewGuid().ToString().Substring(0, 8);

            DateTime issued = DateTime.UtcNow;
            DateTime expiry = issued.AddHours(24);

            var payload = new
            {
                NodeId = nodeId,
                PublicKey = nodePublicKey,
                IssuedAt = issued,
                Expiry = expiry,
                Issuer = "Central-Auth-Server"
            };

            string json = JsonSerializer.Serialize(payload);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(_privateKey);

            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] signature = rsa.SignData(data,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            string sjc = $"{Convert.ToBase64String(data)}|{Convert.ToHexString(signature)}";

            return (nodeId, sjc, expiry);
        }

        // ================= VERIFY =================
        public bool VerifySJC(string sjc)
        {
            try
            {
                var parts = sjc.Split('|');
                if (parts.Length != 2)
                    return false;

                byte[] data = Convert.FromBase64String(parts[0]);
                byte[] signature = Convert.FromHexString(parts[1]);

                using var rsa = RSA.Create();
                rsa.ImportFromPem(_publicKey);

                return rsa.VerifyData(
                    data,
                    signature,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        // ================= GET PUBLIC KEY =================
        public string GetPublicKey()
        {
            return _publicKey;
        }
    }
}