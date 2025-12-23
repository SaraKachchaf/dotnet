using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Prestataire
{
    [ApiController]
    [Route("api/prestataire")]
    [Authorize(Roles = "Prestataire")]
    public class PrestataireStatsController : ControllerBase
    {
        protected string? GetUserId()
        {
            return
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("id")
                ?? User.FindFirstValue("sub");
        }

        private readonly FlowerMarketDbContext _context;

        public PrestataireStatsController(FlowerMarketDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var store = await _context.Stores.AsNoTracking()
                .FirstOrDefaultAsync(s => s.PrestataireId == userId);

            if (store == null)
                return Ok(new { data = new { totalProducts = 0, totalOrders = 0, pendingOrders = 0, totalReviews = 0, averageRating = 0.0, totalRevenue = 0.0 } });

            var storeId = store.Id;

            var totalProducts = await _context.Products.CountAsync(p => p.StoreId == storeId);

            var ordersQuery = _context.Orders.Where(o => o.StoreId == storeId);
            var totalOrders = await ordersQuery.CountAsync();
            var pendingOrders = await ordersQuery.CountAsync(o => o.Status == "pending");
            var totalRevenue = await ordersQuery.SumAsync(o => (double?)o.TotalPrice) ?? 0;

            var reviewsQuery = _context.Reviews.Where(r => r.Product.StoreId == storeId);
            var totalReviews = await reviewsQuery.CountAsync();
            var avgRating = totalReviews == 0 ? 0.0 : await reviewsQuery.AverageAsync(r => (double)r.Rating);

            return Ok(new
            {
                data = new
                {
                    totalProducts,
                    totalOrders,
                    pendingOrders,
                    totalReviews,
                    averageRating = avgRating,
                    totalRevenue
                }
            });
        }
    }
}
