using AuthServer.Models;

namespace AuthServer.Services
{
    public class RegistryService
    {
        private readonly List<NodeRecord> _nodes = new();

        // ================= CHECK =================
        public bool NodeExists(string publicKey)
        {
            return _nodes.Any(n => n.PublicKey == publicKey);
        }

        // ================= ADD =================
        public void AddNode(NodeRecord node)
        {
            _nodes.Add(node);

            Console.WriteLine($"Node registered: {node.NodeId}");
        }

        // ================= GET =================
        public NodeRecord? GetByNodeId(string nodeId)
        {
            return _nodes.FirstOrDefault(n => n.NodeId == nodeId);
        }

        public NodeRecord? GetByPublicKey(string publicKey)
        {
            return _nodes.FirstOrDefault(n => n.PublicKey == publicKey);
        }

        // ================= VALIDATION =================
        public bool IsSJCValid(string nodeId, string sjc)
        {
            var node = GetByNodeId(nodeId);

            if (node == null)
                return false;

            if (node.SJC != sjc)
                return false;

            if (node.ExpiryDate < DateTime.UtcNow)
                return false;

            return true;
        }

        // ================= REMOVE =================
        public void RemoveNode(string nodeId)
        {
            var node = GetByNodeId(nodeId);

            if (node != null)
            {
                _nodes.Remove(node);
                Console.WriteLine($"Node removed: {nodeId}");
            }
        }

        // ================= LIST =================
        public List<NodeRecord> GetAll()
        {
            return _nodes.ToList(); ;
        }
    }
}