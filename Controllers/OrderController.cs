using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wireframe.Data;
using Wireframe.Models;
using Wireframe.Services;
using System.Text.Json;

namespace Wireframe.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ApplicationDbContext _context;

        public OrderController(IOrderService orderService, IProductService productService, ApplicationDbContext context)
        {
            _orderService = orderService;
            _productService = productService;
            _context = context;
        }

        public async Task<IActionResult> Index(string customer, string fromDate, string toDate)
        {
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(customer))
            {
                query = query.Where(o => o.CustomerName.Contains(customer));
            }

            if (!string.IsNullOrWhiteSpace(fromDate) && DateOnly.TryParse(fromDate, out var from))
            {
                query = query.Where(o => o.OrderDate >= from);
            }

            if (!string.IsNullOrWhiteSpace(toDate) && DateOnly.TryParse(toDate, out var to))
            {
                query = query.Where(o => o.OrderDate <= to);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> AddOrder()
        {
            var products = await _productService.GetProductsAsync();
            ViewBag.Products = products;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            return Json(new { success = true, product });
        }

        [HttpPost]
        public async Task<IActionResult> AddOrder([FromBody] OrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var order = new Order
                {
                    CustomerName = model.CustomerName,
                    OrderDate = DateOnly.FromDateTime(DateTime.Now),
                    Total = model.Items.Sum(i => i.Price * i.Quantity)
                };

                var orderItems = model.Items.Select(item => new OrderItem
                {
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity
                }).ToList();

                var createdOrder = await _orderService.CreateOrderAsync(order, orderItems);

                if (createdOrder != null)
                {
                    return Json(new { success = true, message = "Order created successfully!", orderId = createdOrder.Id });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to create order." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred while creating the order: {ex.Message}" });
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var success = await _orderService.UpdateOrderAsync(order);
                if (!success)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _orderService.DeleteOrderAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }

    public class OrderViewModel
    {
        public string CustomerName { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
