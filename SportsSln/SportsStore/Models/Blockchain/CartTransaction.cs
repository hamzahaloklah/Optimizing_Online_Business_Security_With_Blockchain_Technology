namespace SportsStore.Models.Blockchain
{
    public class CartTransaction
    {
        public string Action { get; set; }   
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public string UserSession { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
