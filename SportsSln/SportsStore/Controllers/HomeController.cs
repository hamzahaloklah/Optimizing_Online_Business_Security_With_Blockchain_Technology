using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;
using SportsStore.Models.ViewModels;
using SportsStore.Services;

namespace SportsStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStoreRepository _repository;
        private readonly BlockchainService _blockchainService;
        private readonly NodeService _nodeService;

        public int PageSize = 4;

        public HomeController(
            IStoreRepository repo,
            BlockchainService blockchainService,
            NodeService nodeService)
        {
            _repository = repo;
            _blockchainService = blockchainService;
            _nodeService = nodeService;
        }

        // ================= UI =================
        public ViewResult Index(string? category, int productPage = 1)
        {
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

        // ================= Blockchain Explorer =================
        public IActionResult BlockchainExplorer()
        {
            return View(_blockchainService.GetBlockchain());
        }

        // ================= Integrity Check =================
        public IActionResult CheckIntegrity()
        {
            bool valid = _blockchainService.IsChainValid();

            return Json(new
            {
                Node = _nodeService.CurrentNode.NodeId,
                Valid = valid,
                Message = valid
                    ? "Blockchain integrity verified."
                    : "WARNING: Data manipulation detected."
            });
        }

        // ================= Node Info =================
        public IActionResult NodeInfo()
        {
            return Json(new
            {
                NodeId = _nodeService.CurrentNode.NodeId,
                IsClusterHead = _nodeService.CurrentNode.IsClusterHead,
                TrustWeight = _nodeService.CalculateWeight(_nodeService.CurrentNode)
            });
        }
    }
}