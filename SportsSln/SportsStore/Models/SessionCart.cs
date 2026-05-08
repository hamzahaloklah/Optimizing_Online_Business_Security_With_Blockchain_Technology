using System.Text.Json.Serialization;
using SportsStore.Infrastructure;
using SportsStore.Services;
using SportsStore.Models.Blockchain;

namespace SportsStore.Models
{
    public class SessionCart : Cart
    {
        [JsonIgnore]
        public ISession? Session { get; set; }

        [JsonIgnore]
        private BlockchainService? _blockchainService;

        [JsonIgnore]
        private NodeCommunicationService? _nodeComm;

        public static Cart GetCart(IServiceProvider services)
        {
            ISession? session = services
                .GetRequiredService<IHttpContextAccessor>().HttpContext?.Session;

            var cart = session?.GetJson<SessionCart>("Cart") ?? new SessionCart();

            cart.Session = session;
            cart._blockchainService = services.GetRequiredService<BlockchainService>();
            cart._nodeComm = services.GetRequiredService<NodeCommunicationService>();

            return cart;
        }

        public override void AddItem(Product product, int quantity)
        {
            base.AddItem(product, quantity);
            Session?.SetJson("Cart", this);

            var tx = new CartTransaction
            {
                Action = "Add",
                ProductId = (long)product.ProductID,
                Quantity = quantity,
                UserSession = Session?.Id ?? "NoSession",
                Timestamp = DateTime.UtcNow
            };

            
            _nodeComm!.BroadcastTransactionAsync(tx);
        }

        public override void RemoveLine(Product product)
        {
            base.RemoveLine(product);
            Session?.SetJson("Cart", this);

            var tx = new CartTransaction
            {
                Action = "Remove",
                ProductId = (long)product.ProductID,
                Quantity = 0,
                UserSession = Session?.Id ?? "NoSession",
                Timestamp = DateTime.UtcNow
            };

           
            _nodeComm!.BroadcastTransactionAsync(tx);
        }

        public override void Clear()
        {
            base.Clear();
            Session?.Remove("Cart");

            var tx = new CartTransaction
            {
                Action = "Clear",
                ProductId = 0,
                Quantity = 0,
                UserSession = Session?.Id ?? "NoSession",
                Timestamp = DateTime.UtcNow
            };

            
            _nodeComm!.BroadcastTransactionAsync(tx);
        }
    }
}
