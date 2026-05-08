using SportsStore.Models.Node;

namespace SportsStore.Services
{
    public class NodeService
    {
        public NodeInfo CurrentNode { get; private set; }

        public string PrivateKey { get; private set; }
        public string PublicKey { get; private set; }

        public List<NodeInfo> ClusterNodes { get; private set; } = new();

        private readonly HttpClient _httpClient = new HttpClient();

        public NodeService(KeyGeneratorService keyService)
        {
            PrivateKey = keyService.PrivateKey;
            PublicKey = keyService.PublicKey;

            CurrentNode = new NodeInfo
            {
                NodeId = "",
                NodeName = "Unregistered Node",
                Address = "",
                InitialWeight = 0.5,
                IncentiveProfit = 0,
                IsClusterHead = false,

                // 🔥 جديد
                CorrectVotes = 0,
                TotalVotes = 0
            };

            ClusterNodes.Add(CurrentNode);
        }

        // ================= SET ID =================
        public void SetCurrentNodeIdentity(string nodeId, string address)
        {
            CurrentNode.NodeId = nodeId;
            CurrentNode.Address = address;

            if (ClusterNodes.Count == 1)
            {
                CurrentNode.IsClusterHead = true;
                Console.WriteLine("This node is Cluster Head");
            }

            Console.WriteLine($"Node identity set: {nodeId}");
        }

        // ================= REGISTER =================
        public bool RegisterNode(NodeInfo node)
        {
            if (ClusterNodes.Any(n => n.NodeId == node.NodeId))
                return false;

            node.InitialWeight = 0.5;
            node.IncentiveProfit = 0;

            // 🔥 جديد
            node.CorrectVotes = 0;
            node.TotalVotes = 0;

            ClusterNodes.Add(node);

            Console.WriteLine($"Node registered: {node.NodeId}");
            return true;
        }

        // ================= GET HEAD =================
        public NodeInfo? GetClusterHead()
        {
            return ClusterNodes.FirstOrDefault(n => n.IsClusterHead);
        }

        // ================= SET HEAD =================
        public void SetClusterHead(string nodeId)
        {
            foreach (var node in ClusterNodes)
                node.IsClusterHead = false;

            var leader = ClusterNodes.FirstOrDefault(n => n.NodeId == nodeId);

            if (leader != null)
            {
                leader.IsClusterHead = true;

                if (CurrentNode.NodeId == leader.NodeId)
                    CurrentNode.IsClusterHead = true;

                Console.WriteLine($"🔥 New Cluster Head: {leader.NodeId}");
            }
        }

        // ================= DYNAMIC WEIGHT =================
        public double CalculateWeight(NodeInfo node)
        {
            // 🔥 المعادلة الجديدة
            return (node.CorrectVotes + 1.0) / (node.TotalVotes + 2.0);
        }

        // ================= UPDATE AFTER VOTE =================
        public void UpdateNodeTrust(string nodeId, bool votedCorrectly)
        {
            var node = ClusterNodes.FirstOrDefault(n => n.NodeId == nodeId);

            if (node == null) return;

            if (votedCorrectly)
                node.CorrectVotes++;

            node.TotalVotes++;
        }

        // ================= UPDATE LEADER =================
        public void UpdateClusterLeadership()
        {
            if (!ClusterNodes.Any())
                return;

            var leader = ClusterNodes
                .OrderByDescending(n => CalculateWeight(n))
                .First();

            SetClusterHead(leader.NodeId);
        }

        // ================= CHECK IF NODE ALIVE =================
        public async Task<bool> IsNodeAlive(string address)
        {
            try
            {
                var res = await _httpClient.GetAsync($"{address}/api/node/ping");
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ================= CHECK HEAD =================
        public async Task CheckAndRecoverClusterHead()
        {
            var head = GetClusterHead();

            if (head == null)
                return;

            bool alive = await IsNodeAlive(head.Address);

            if (!alive)
            {
                Console.WriteLine("⚠️ Cluster Head down → Electing new leader...");
                UpdateClusterLeadership();
            }
        }
    }
}