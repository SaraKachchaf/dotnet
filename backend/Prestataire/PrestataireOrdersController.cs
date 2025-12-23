using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Prestataire
{
    [ApiController]
    [Route("api/prestataire/orders")]
    [Authorize(Roles = "Prestataire")]
    public class PrestataireOrdersController : ControllerBase
    {
        protected string? GetUserId()
        {
            return
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("id")
                ?? User.FindFirstValue("sub");
        }

        private readonly FlowerMarketDbContext _context;

        public PrestataireOrdersController(FlowerMarketDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var store = await _context.Stores.AsNoTracking()
                .FirstOrDefaultAsync(s => s.PrestataireId == userId);

            if (store == null) return Ok(new { data = new List<object>() });

            var orders = await _context.Orders
                .Where(o => o.StoreId == store.Id)
                .Include(o => o.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    id = o.Id,
                    createdAt = o.CreatedAt,
                    status = o.Status,
                    totalAmount = o.TotalPrice,
                    customerName = o.User.FullName,
                    customerEmail = o.User.Email,
                    productName = o.Product.Name,
                    quantity = o.Quantity
                })
                .ToListAsync();

            return Ok(new { data = orders });
        }

        public class UpdateOrderStatusRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string Status { get; set; }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Status)) return BadRequest(new { error = "status is required" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Vérifier que c'est bien le Prestataire lié au produit de la commande
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.PrestataireId == userId);

            if (store == null) return BadRequest(new { error = "Store not found." });

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.StoreId == store.Id);

            if (order == null) return NotFound(new { error = "Order not found." });

            order.Status = request.Status;
            await _context.SaveChangesAsync();

            return Ok(new { data = order });
        }

    }

}
