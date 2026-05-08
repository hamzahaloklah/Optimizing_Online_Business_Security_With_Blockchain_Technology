namespace SportsStore.Models.Node
{
    public class NodeInfo
    {
        public string NodeId { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public DateTime JoiningTime { get; set; } = DateTime.UtcNow;

        public double InitialWeight { get; set; } = 0.5;
        public double IncentiveProfit { get; set; } = 0.0;

        public double Age => (DateTime.UtcNow - JoiningTime).TotalHours;

        public bool IsClusterHead { get; set; } = false;
        public int CorrectVotes { get; set; } = 0;
        public int TotalVotes { get; set; } = 0;
    }
}