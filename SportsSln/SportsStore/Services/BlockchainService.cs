using SportsStore.Models.Blockchain;
using SportsStore.Models.Node;

namespace SportsStore.Services
{
    public class BlockchainService
    {
        private static List<Block> _chain = new List<Block> { CreateGenesisBlock() };

        private readonly NodeService _nodeService;

        public BlockchainService(NodeService nodeService)
        {
            _nodeService = nodeService;
        }

        // ===== Genesis =====
        private static Block CreateGenesisBlock()
        {
            var genesis = new Block
            {
                Index = 0,
                Timestamp = new DateTime(2026, 1, 1),
                PreviousHash = "0",
                NodeId = "Genesis-CH",
                NodeName = "Root Cluster Head",
                PublicKey = "SYSTEM_KEY",
                IPAddress = "127.0.0.1",
                Address = "local",
                TrustWeight = 1.0,
                DigitalSignature = "GENESIS"
            };

            genesis.Hash = genesis.CalculateHash();
            return genesis;
        }

        public List<Block> GetBlockchain() => _chain;

        // ===== إضافة بلوك =====
        public bool AddBlock(Block block)
        {
            if (!_nodeService.CurrentNode.IsClusterHead)
            {
                Console.WriteLine("Only Cluster Head can add blocks");
                return false;
            }

            if (!ValidateBlock(block))
            {
                Console.WriteLine("Block validation failed");
                return false;
            }

            _chain.Add(block);

            Console.WriteLine($"Block {block.NodeId} added. Chain length: {_chain.Count}");

            return true;
        }

        // ===== تحقق من البلوك =====
        public bool ValidateBlock(Block block)
        {
            var last = _chain.Last();

            if (block.PreviousHash != last.Hash)
                return false;

            if (block.Hash != block.CalculateHash())
                return false;

            if (!block.IsValidStructure())
                return false;

            if (!block.VerifySignature())
                return false;

            return true;
        }

        // ===== تحقق من السلسلة =====
        public bool IsChainValid()
        {
            for (int i = 1; i < _chain.Count; i++)
            {
                var current = _chain[i];
                var previous = _chain[i - 1];

                if (current.PreviousHash != previous.Hash)
                    return false;

                if (current.Hash != current.CalculateHash())
                    return false;

                if (!current.VerifySignature())
                    return false;
            }

            return true;
        }

        // ===== استبدال السلسلة (sync) =====
        public void ReplaceChain(List<Block> newChain)
        {
            if (newChain.Count >= _chain.Count && ValidateFullChain(newChain))
            {
                _chain = newChain;
                Console.WriteLine("Chain replaced with longer valid chain");
            }
        }

        // ===== تحقق كامل =====
        private bool ValidateFullChain(List<Block> chain)
        {
            for (int i = 1; i < chain.Count; i++)
            {
                if (chain[i].PreviousHash != chain[i - 1].Hash)
                    return false;

                if (chain[i].Hash != chain[i].CalculateHash())
                    return false;

                if (!chain[i].VerifySignature())
                    return false;
            }

            return true;
        }

        // ===== مكافأة =====
        public void RewardNode(string nodeId)
        {
            var node = _nodeService.ClusterNodes.FirstOrDefault(n => n.NodeId == nodeId);

            if (node != null)
            {
                node.IncentiveProfit += 0.05;

                Console.WriteLine($"Node {nodeId} rewarded");

                _nodeService.UpdateClusterLeadership();
            }
        }
    }
}