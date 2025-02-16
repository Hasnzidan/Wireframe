using Microsoft.EntityFrameworkCore;
using Wireframe.Data;
using Wireframe.Models;

namespace Wireframe.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetProductsAsync(string searchName = null, DateOnly? fromDate = null, DateOnly? toDate = null);
        Task<Product> GetProductByIdAsync(int id);
        Task<Product> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
    }

    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetProductsAsync(string searchName = null, DateOnly? fromDate = null, DateOnly? toDate = null)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                searchName = searchName.Trim();
                query = query.Where(p => p.Name.ToLower().Contains(searchName.ToLower()));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.Date >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.Date <= toDate.Value);
            }

            return await query.OrderByDescending(p => p.Date).ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            _context.Entry(product).State = EntityState.Modified;
            
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Products.AnyAsync(e => e.Id == product.Id))
                {
                    return false;
                }
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return false;
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
