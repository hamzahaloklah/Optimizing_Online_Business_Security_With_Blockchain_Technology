using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthServer.Models;
using AuthServer.Services;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly RegistryService _registry;

        public AuthController(AuthService authService, RegistryService registry)
        {
            _authService = authService;
            _registry = registry;
        }

        // ================= REGISTER =================
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.PublicKey))
                return BadRequest("Public key required");

            if (_registry.NodeExists(request.PublicKey))
                return BadRequest("Node already registered");

            // 🔥 توليد الهوية + SJC
            var result = _authService.GenerateIdentity(request.PublicKey);

            string nodeId = result.NodeId;
            string sjc = result.SJC;
            DateTime expiry = result.Expiry;

            // 🔥 الحصول على IP الحقيقي
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // 🔥 تخزين العقدة
            _registry.AddNode(new NodeRecord
            {
                NodeId = nodeId,
                NodeName = request.NodeName,
                PublicKey = request.PublicKey,
                Address = request.Address,
                IPAddress = ip,
                SJC = sjc,
                RegisteredAt = DateTime.UtcNow,
                ExpiryDate = expiry
            });

            return Ok(new RegisterResponse
            {
                NodeId = nodeId,
                SJC = sjc,
                ExpiryDate = expiry,
                ServerPublicKey = _authService.GetPublicKey()
            });
        }

        // ================= VERIFY =================
        [HttpPost("verify")]
        public IActionResult Verify([FromBody] VerifyRequest request)
        {
            var node = _registry.GetByNodeId(request.NodeId);

            if (node == null)
                return BadRequest("Node not found");

            // 🔐 تحقق من SJC
            if (!_authService.VerifySJC(request.SJC))
                return Ok(new { Valid = false, Message = "Invalid SJC" });

            // ⏳ تحقق من الانتهاء
            if (node.ExpiryDate < DateTime.UtcNow)
                return Ok(new { Valid = false, Message = "SJC expired" });

            // 🔐 تحقق من توقيع الطلب
            if (!VerifySignature(request, node.PublicKey))
                return Ok(new { Valid = false, Message = "Invalid signature" });

            return Ok(new { Valid = true, Message = "Verified successfully" });
        }

        // ================= SIGNATURE CHECK =================
        private bool VerifySignature(VerifyRequest request, string publicKey)
        {
            try
            {
                byte[] data = Convert.FromBase64String(request.Data);
                byte[] sig = Convert.FromHexString(request.Signature);

                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKey);

                return rsa.VerifyData(data, sig,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        // ================= DEBUG =================
        [HttpGet("nodes")]
        public IActionResult GetAllNodes()
        {
            return Ok(_registry.GetAll());
        }
        // ================= GET PUBLIC KEY =================
        [HttpGet("publickey/{nodeId}")]
        public IActionResult GetPublicKey(string nodeId)
        {
            var node = _registry.GetByNodeId(nodeId);

            if (node == null)
                return NotFound("Node not found");

            return Ok(new
            {
                NodeId = node.NodeId,
                PublicKey = node.PublicKey
            });
        }
    }
}