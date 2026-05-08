using System.Security.Cryptography;

namespace SportsStore.Services
{
    public class KeyGeneratorService
    {
        public string PrivateKey { get; private set; } = string.Empty;
        public string PublicKey { get; private set; } = string.Empty;

        public KeyGeneratorService()
        {
            GenerateKeys();
        }

        private void GenerateKeys()
        {
            using var rsa = RSA.Create(2048);

            PrivateKey = rsa.ExportPkcs8PrivateKeyPem();
            PublicKey = rsa.ExportSubjectPublicKeyInfoPem();
        }
    }
}