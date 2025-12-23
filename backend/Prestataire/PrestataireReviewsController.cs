using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Prestataire
{
    [ApiController]
    [Route("api/prestataire/reviews")]
    [Authorize(Roles = "Prestataire")]
    public class PrestataireReviewsController : ControllerBase
    {
        protected string? GetUserId()
        {
            return
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("id")
                ?? User.FindFirstValue("sub");
        }

        private readonly FlowerMarketDbContext _context;

        public PrestataireReviewsController(FlowerMarketDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var store = await _context.Stores.AsNoTracking()
                .FirstOrDefaultAsync(s => s.PrestataireId == userId);

            if (store == null) return Ok(new { data = new List<object>() });

            var reviews = await _context.Reviews
                .Where(r => r.Product.StoreId == store.Id)
                .Include(r => r.Product)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdAt = r.CreatedAt,
                    productName = r.Product.Name,
                    customerName = r.User.FullName,
                    customerEmail = r.User.Email
                })
                .ToListAsync();

            return Ok(new { data = reviews });
        }
    }
}
