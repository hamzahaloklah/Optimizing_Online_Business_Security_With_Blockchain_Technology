using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SportsStore.Models.Blockchain;
using SportsStore.Models.Node;

namespace SportsStore.Services
{
    public class NodeCommunicationService
    {
        private readonly HttpClient _httpClient;
        private readonly NodeService _nodeService;
        private readonly KeyGeneratorService _keyService;

        private const string AUTH_SERVER = "http://localhost:4000";

        public NodeCommunicationService(
            HttpClient httpClient,
            NodeService nodeService,
            KeyGeneratorService keyService)
        {
            _httpClient = httpClient;
            _nodeService = nodeService;
            _keyService = keyService;
        }

        // ================= SECURE MESSAGE =================
        public class SecureMessage
        {
            public string EncryptedData { get; set; } = "";
            public string EncryptedKey { get; set; } = "";
            public string Signature { get; set; } = "";
            public string PublicKey { get; set; } = "";
            public DateTime Timestamp { get; set; }
        }

        // ================= GET TARGET PUBLIC KEY =================
        private async Task<string?> GetTargetPublicKeyAsync(string nodeId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<PublicKeyResponse>(
                    $"{AUTH_SERVER}/api/auth/publickey/{nodeId}");

                return response?.PublicKey;
            }
            catch
            {
                return null;
            }
        }

        private class PublicKeyResponse
        {
            public string NodeId { get; set; } = "";
            public string PublicKey { get; set; } = "";
        }

        // ================= ENCRYPT =================
        public async Task<SecureMessage?> CreateSecureMessageAsync(object payload, string targetNodeId)
        {
            try
            {
                string json = JsonSerializer.Serialize(payload);
                byte[] dataBytes = Encoding.UTF8.GetBytes(json);

                // ===== AES =====
                using var aes = Aes.Create();
                aes.GenerateKey();
                aes.GenerateIV();

                byte[] encryptedData;
                using (var encryptor = aes.CreateEncryptor())
                {
                    encryptedData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                }

                byte[] combined = new byte[aes.IV.Length + encryptedData.Length];
                Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
                Buffer.BlockCopy(encryptedData, 0, combined, aes.IV.Length, encryptedData.Length);

                // ===== GET PUBLIC KEY FROM SERVER =====
                string? targetPublicKey = await GetTargetPublicKeyAsync(targetNodeId);

                if (string.IsNullOrEmpty(targetPublicKey))
                    throw new Exception("Target public key not found");

                // ===== RSA ENCRYPT AES KEY =====
                using var rsa = RSA.Create();
                rsa.ImportFromPem(targetPublicKey);

                byte[] encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);

                // ===== SIGN =====
                using var rsaSign = RSA.Create();
                rsaSign.ImportFromPem(_keyService.PrivateKey);

                byte[] signature = rsaSign.SignData(
                    dataBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                return new SecureMessage
                {
                    EncryptedData = Convert.ToBase64String(combined),
                    EncryptedKey = Convert.ToBase64String(encryptedKey),
                    Signature = Convert.ToHexString(signature),
                    PublicKey = _keyService.PublicKey,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }

        // ================= CLUSTER HEAD =================
        private string GetClusterHeadAddress()
        {
            var head = _nodeService.GetClusterHead();

            if (head == null || string.IsNullOrEmpty(head.Address))
                throw new Exception("Cluster Head not available");

            return head.Address;
        }

        // ================= REQUEST VOTE =================
        public async Task<VoteResult?> RequestVoteAsync(Block block)
        {
            try
            {
                var head = _nodeService.GetClusterHead();

                var message = await CreateSecureMessageAsync(block, head.NodeId);

                if (message == null)
                    return null;

                var response = await _httpClient.PostAsJsonAsync(
                    $"{head.Address}/api/node/request-vote",
                    message);

                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<VoteResult>();
            }
            catch
            {
                return null;
            }
        }

        // ================= COMMIT =================
        public async Task<bool> CommitBlockAsync(Block block)
        {
            try
            {
                var head = _nodeService.GetClusterHead();

                var message = await CreateSecureMessageAsync(block, head.NodeId);

                if (message == null)
                    return false;

                var response = await _httpClient.PostAsJsonAsync(
                    $"{head.Address}/api/node/commit-block",
                    message);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ================= TRANSACTION =================
        public async Task BroadcastTransactionAsync(CartTransaction tx)
        {
            try
            {
                var head = _nodeService.GetClusterHead();

                var message = await CreateSecureMessageAsync(tx, head.NodeId);

                if (message == null)
                    return;

                await _httpClient.PostAsJsonAsync(
                    $"{head.Address}/api/node/receive-tx",
                    message);
            }
            catch
            {
                Console.WriteLine("Transaction failed");
            }
        }

        // ================= GET CHAIN =================
        public async Task<List<Block>> GetChainAsync()
        {
            try
            {
                var address = GetClusterHeadAddress();

                return await _httpClient.GetFromJsonAsync<List<Block>>(
                    $"{address}/api/node/explorer")
                    ?? new List<Block>();
            }
            catch
            {
                return new List<Block>();
            }
        }

        // ================= RESULT =================
        public class VoteResult
        {
            public bool Approved { get; set; }
            public double Weight { get; set; }
        }
    }
}