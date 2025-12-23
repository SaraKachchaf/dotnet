using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Prestataire
{
    [ApiController]
    [Authorize(Roles = "Prestataire")]
    [Route("api/prestataire/products")]
    public class PrestataireProductsController : ControllerBase
    {
        protected string? GetUserId()
        {
            return
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("id")
                ?? User.FindFirstValue("sub");
        }

        private readonly FlowerMarketDbContext _context;

        public PrestataireProductsController(FlowerMarketDbContext context)
        {
            _context = context;
        }

        private async Task<Store?> GetMyStore()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _context.Stores
                .FirstOrDefaultAsync(s => s.PrestataireId == userId);
        }



        // POST /api/prestataire/products
        [HttpPost]
        public async Task<IActionResult> CreateProduct(
          [FromForm] string name,
          [FromForm] decimal price,
          [FromForm] int stock,
          [FromForm] string category,
          [FromForm] string description,
          [FromForm] string? imageUrl,
          [FromForm] IFormFile? image
      )
        {
            var store = await GetMyStore();
            if (store == null)
                return BadRequest("Store introuvable");

            string? finalImagePath = imageUrl;

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads"
                );

                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                finalImagePath = "/uploads/" + fileName;
            }

            var product = new Product
            {
                Name = name,
                Price = (double)price,
                Stock = stock,
                Category = category,
                Description = description,
                ImageUrl = finalImagePath,
                StoreId = store.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new { data = product });
        }



        // PUT /api/prestataire/products/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product dto)
        {
            var store = await GetMyStore();
            if (store == null) return BadRequest("Store introuvable");

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.StoreId == store.Id);

            if (product == null)
                return NotFound("Produit introuvable");

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.Category = dto.Category;
            product.Description = dto.Description;
            product.ImageUrl = dto.ImageUrl;
            product.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { data = product });
        }

        // DELETE /api/prestataire/products/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var store = await GetMyStore();
            if (store == null) return BadRequest("Store introuvable");

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.StoreId == store.Id);

            if (product == null)
                return NotFound("Produit introuvable");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Produit supprimé" });
        }
        [HttpGet]
        public async Task<IActionResult> GetMyProducts()
        {
            try
            {
                var store = await GetMyStore();
                if (store == null)
                    return Ok(new { data = new List<Product>() });

                var products = await _context.Products
                    .Where(p => p.StoreId == store.Id)
                    .ToListAsync();

                return Ok(new { data = products });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }


    }
}
