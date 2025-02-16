using Microsoft.EntityFrameworkCore;
using Wireframe.Data;
using Wireframe.Models;

namespace Wireframe.Services
{
    public interface IOrderService
    {
        Task<List<Order>> GetOrdersAsync();
        Task<Order> GetOrderByIdAsync(int id);
        Task<Order> CreateOrderAsync(Order order, List<OrderItem> items);
        Task<bool> UpdateOrderAsync(Order order);
        Task<bool> DeleteOrderAsync(int id);
    }

    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetOrdersAsync()
        {
            return await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            return await _context.Orders.FindAsync(id);
        }

        public async Task<Order> CreateOrderAsync(Order order, List<OrderItem> items)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                order.OrderDate = DateOnly.FromDateTime(DateTime.Now);
                order.Total = items.Sum(i => i.Price * i.Quantity);

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                foreach (var item in items)
                {
                    item.OrderId = order.Id;
                    item.Order = order;
                }

                await _context.OrderItems.AddRangeAsync(items);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateOrderAsync(Order order)
        {
            _context.Entry(order).State = EntityState.Modified;
            
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Orders.AnyAsync(e => e.Id == order.Id))
                {
                    return false;
                }
                throw;
            }
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return false;
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
