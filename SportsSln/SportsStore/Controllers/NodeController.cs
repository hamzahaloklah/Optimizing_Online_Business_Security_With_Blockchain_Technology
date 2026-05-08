using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using SportsStore.Models;
using SportsStore.Models.Blockchain;
using SportsStore.Models.Node;
using SportsStore.Models.ViewModels;
using SportsStore.Services;

namespace SportsStore.Controllers
{
    public class NodeController : Controller
    {
        private readonly NodeService _nodeService;
        private readonly BlockchainService _blockchainService;
        private readonly IStoreRepository _repository;
        private readonly NodeCommunicationService _commService;
        private readonly HttpClient _httpClient;

        public NodeController(
            NodeService nodeService,
            BlockchainService blockchainService,
            IStoreRepository repository,
            NodeCommunicationService commService,
            IHttpClientFactory httpClientFactory)
        {
            _nodeService = nodeService;
            _blockchainService = blockchainService;
            _repository = repository;
            _commService = commService;
            _httpClient = httpClientFactory.CreateClient();
        }

        // ================= UI =================
        [HttpGet]
        public IActionResult Index(string? category, int productPage = 1)
        {
            int PageSize = 4;

            return View(new ProductListViewModel
            {
                Products = _repository.Products
                    .Where(p => category == null || p.Category == category)
                    .OrderBy(p => p.ProductID)
                    .Skip((productPage - 1) * PageSize)
                    .Take(PageSize),

                PagingInfo = new PagingInfo
                {
                    CurrentPage = productPage,
                    ItemsPerPage = PageSize,
                    TotalItems = category == null
                        ? _repository.Products.Count()
                        : _repository.Products.Count(e => e.Category == category)
                },

                CurrentCategory = category
            });
        }

        [HttpGet("api/node/ping")]
        public IActionResult Ping() => Ok("Alive");

        // ================= DECRYPT =================
        private (Block block, byte[] raw) DecryptSecureMessage(NodeCommunicationService.SecureMessage msg)
        {
            var keyService = HttpContext.RequestServices.GetRequiredService<KeyGeneratorService>();

            using var rsa = RSA.Create();
            rsa.ImportFromPem(keyService.PrivateKey);

            byte[] aesKey = rsa.Decrypt(Convert.FromBase64String(msg.EncryptedKey), RSAEncryptionPadding.Pkcs1);

            byte[] combined = Convert.FromBase64String(msg.EncryptedData);
            byte[] iv = combined.Take(16).ToArray();
            byte[] cipher = combined.Skip(16).ToArray();

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            var block = JsonSerializer.Deserialize<Block>(Encoding.UTF8.GetString(plain))!;

            return (block, plain);
        }

        // ================= VERIFY SIGNATURE =================
        private bool VerifySignature(NodeCommunicationService.SecureMessage msg, byte[] raw)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(msg.PublicKey);

                return rsa.VerifyData(
                    raw,
                    Convert.FromHexString(msg.Signature),
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        // ================= VERIFY IDENTITY =================
        private async Task<bool> VerifyIdentityAsync(string nodeId, string publicKey)
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<PublicKeyResponse>(
                    $"http://localhost:4000/api/auth/publickey/{nodeId}");

                return res != null && res.PublicKey == publicKey;
            }
            catch
            {
                return false;
            }
        }

        private class PublicKeyResponse
        {
            public string NodeId { get; set; } = "";
            public string PublicKey { get; set; } = "";
        }

        // ================= JOIN =================
        [HttpPost("api/node/join-network")]
        public async Task<IActionResult> JoinNetwork([FromServices] KeyGeneratorService keyService)
        {
            var registerRequest = new
            {
                PublicKey = keyService.PublicKey,
                NodeName = "Node-App",
                Address = $"http://localhost:{HttpContext.Request.Host.Port}",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
            };

            var response = await _httpClient.PostAsJsonAsync(
                "http://localhost:4000/api/auth/register",
                registerRequest);

            if (!response.IsSuccessStatusCode)
                return BadRequest("Auth Server failed");

            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            _nodeService.SetCurrentNodeIdentity(result.NodeId, registerRequest.Address);
            var syncedChain = await _commService.GetChainAsync();

            if (syncedChain.Any())
            {
                _blockchainService.ReplaceChain(syncedChain);
            }

            // ================= SYNC NETWORK =================
            var allNodes = await _httpClient.GetFromJsonAsync<List<NodeRecordDto>>(
                "http://localhost:4000/api/auth/nodes");

            if (allNodes != null)
            {
                foreach (var n in allNodes)
                {
                    // تجاهل نفسها
                    if (n.NodeId == result.NodeId)
                        continue;

                    _nodeService.RegisterNode(new NodeInfo
                    {
                        NodeId = n.NodeId,
                        NodeName = n.NodeName,
                        Address = n.Address,
                        IsClusterHead = false
                    });
                }
            }
            var chain = _blockchainService.GetBlockchain();
            var lastBlock = chain.Last();

            var block = new Block
            {
                Index = chain.Count,
                PreviousHash = lastBlock.Hash,
                NodeId = result.NodeId,
                NodeName = "Authenticated Node",
                PublicKey = registerRequest.PublicKey,
                Address = registerRequest.Address,
                IPAddress = registerRequest.IPAddress,
                TrustWeight = 0.5,
                Timestamp = DateTime.UtcNow,
                DigitalSignature = result.SJC
            };

            block.Hash = block.CalculateHash();

            var voteResult = await _commService.RequestVoteAsync(block);

            return Ok(voteResult);
        }

        // ================= CLUSTER HEAD =================
        [HttpPost("api/node/request-vote")]
        public async Task<IActionResult> HandleVoteRequest([FromBody] NodeCommunicationService.SecureMessage msg)
        {
            await _nodeService.CheckAndRecoverClusterHead();

            var (candidate, raw) = DecryptSecureMessage(msg);

            if (!VerifySignature(msg, raw))
                return BadRequest("Invalid signature");

            if (!await VerifyIdentityAsync(candidate.NodeId, msg.PublicKey))
                return BadRequest("Untrusted node");

            if (!_nodeService.CurrentNode.IsClusterHead)
                return BadRequest("Not Cluster Head");

            var nodes = _nodeService.ClusterNodes;

            double totalWeight = nodes.Sum(n => _nodeService.CalculateWeight(n));
            double approvalWeight = _nodeService.CalculateWeight(_nodeService.CurrentNode);

            var votes = new Dictionary<string, bool>();

            foreach (var node in nodes)
            {
                if (node.NodeId == _nodeService.CurrentNode.NodeId)
                    continue;

                try
                {
                    var secure = await _commService.CreateSecureMessageAsync(candidate, node.NodeId);

                    var res = await _httpClient.PostAsJsonAsync(
                        $"{node.Address}/api/node/vote-block",
                        secure);

                    var vote = await res.Content.ReadFromJsonAsync<VoteResult>();

                    if (vote != null)
                    {
                        votes[node.NodeId] = vote.Approved;

                        if (vote.Approved)
                            approvalWeight += vote.Weight;
                    }
                }
                catch { }
            }

            double percentage = (approvalWeight / totalWeight) * 100;
            bool finalDecision = percentage > 50;

            // 🔥 تحديث الأوزان
            foreach (var node in nodes)
            {
                if (node.NodeId == _nodeService.CurrentNode.NodeId)
                    continue;

                if (votes.ContainsKey(node.NodeId))
                {
                    bool correct = votes[node.NodeId] == finalDecision;
                    _nodeService.UpdateNodeTrust(node.NodeId, correct);
                }
            }

            // 🔥 تحديث القائد
            _nodeService.UpdateClusterLeadership();

            if (finalDecision)
            {
                _blockchainService.GetBlockchain().Add(candidate);

                foreach (var node in nodes)
                {
                    if (node.NodeId == _nodeService.CurrentNode.NodeId)
                        continue;

                    try
                    {
                        var secure = await _commService.CreateSecureMessageAsync(candidate, node.NodeId);

                        await _httpClient.PostAsJsonAsync(
                            $"{node.Address}/api/node/commit-block",
                            secure);
                    }
                    catch { }
                }

                return Ok(new { Approved = true, Weight = approvalWeight, Percentage = percentage });
            }

            return Ok(new { Approved = false, Weight = approvalWeight, Percentage = percentage });
        }

        // ================= NODE VOTE =================
        [HttpPost("api/node/vote-block")]
        public async Task<IActionResult> Vote([FromBody] NodeCommunicationService.SecureMessage msg)
        {
            var (block, raw) = DecryptSecureMessage(msg);

            if (!VerifySignature(msg, raw))
                return BadRequest("Invalid signature");

            if (!await VerifyIdentityAsync(block.NodeId, msg.PublicKey))
                return BadRequest("Untrusted node");

            return Ok(new
            {
                Approved = block.VerifySignature(),
                Weight = _nodeService.CalculateWeight(_nodeService.CurrentNode)
            });
        }

        // ================= COMMIT =================
        [HttpPost("api/node/commit-block")]
        public async Task<IActionResult> Commit([FromBody] NodeCommunicationService.SecureMessage msg)
        {
            var (block, raw) = DecryptSecureMessage(msg);

            if (!VerifySignature(msg, raw))
                return BadRequest("Invalid signature");

            if (!await VerifyIdentityAsync(block.NodeId, msg.PublicKey))
                return BadRequest("Untrusted node");

            var chain = _blockchainService.GetBlockchain();

            if (!chain.Any(b => b.Hash == block.Hash))
                chain.Add(block);

            return Ok();
        }

        [HttpGet("api/node/explorer")]
        public IActionResult Explorer()
        {
            return Ok(_blockchainService.GetBlockchain());
        }
        private class NodeRecordDto
        {
            public string NodeId { get; set; } = "";
            public string NodeName { get; set; } = "";
            public string Address { get; set; } = "";
        }
        private class VoteResult
        {
            public bool Approved { get; set; }
            public double Weight { get; set; }
        }
    }
}