using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wireframe.Data;
using Wireframe.Models;
using Wireframe.Services;
using System.Text.Json;
using Wireframe.Helpers;

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

        public async Task<IActionResult> Index(string customer, string fromDate, string toDate, int? pageNumber)
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

            const int pageSize = 4; 
            query = query.OrderByDescending(o => o.OrderDate);
            
            // Ensure pageNumber is valid
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }
            
            var orders = await PaginatedList<Order>.CreateAsync(query, pageNumber ?? 1, pageSize);

            ViewData["CurrentFilter"] = customer;
            ViewData["FromDate"] = fromDate;
            ViewData["ToDate"] = toDate;
            ViewData["CurrentPage"] = pageNumber ?? 1;

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
                    ProductId = item.ProductId,
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
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var products = await _context.Products.ToListAsync();
            ViewBag.Products = products;

            var viewModel = new OrderViewModel
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                OrderDate = order.OrderDate,
                Items = order.OrderItems.Select(item => new OrderItemViewModel
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, OrderViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.Id == id);

                    if (order == null)
                    {
                        return NotFound();
                    }

                    order.CustomerName = viewModel.CustomerName;
                    order.OrderDate = viewModel.OrderDate;

                    // Remove deleted items
                    var existingItemIds = viewModel.Items.Where(i => i.Id > 0).Select(i => i.Id);
                    var itemsToRemove = order.OrderItems.Where(i => !existingItemIds.Contains(i.Id)).ToList();
                    foreach (var item in itemsToRemove)
                    {
                        _context.OrderItems.Remove(item);
                    }

                    // Update existing and add new items
                    foreach (var itemVM in viewModel.Items)
                    {
                        if (itemVM.Id > 0)
                        {
                            // Update existing item
                            var existingItem = order.OrderItems.First(i => i.Id == itemVM.Id);
                            existingItem.ProductId = itemVM.ProductId;
                            existingItem.ProductName = itemVM.ProductName;
                            existingItem.Price = itemVM.Price;
                            existingItem.Quantity = itemVM.Quantity;
                        }
                        else
                        {
                            // Add new item
                            order.OrderItems.Add(new OrderItem
                            {
                                ProductId = itemVM.ProductId,
                                ProductName = itemVM.ProductName,
                                Price = itemVM.Price,
                                Quantity = itemVM.Quantity
                            });
                        }
                    }

                    // Calculate total
                    order.Total = order.OrderItems.Sum(i => i.Price * i.Quantity);

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var products = await _context.Products.ToListAsync();
            ViewBag.Products = products;
            return View(viewModel);
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _orderService.DeleteOrderAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }

    public class OrderViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public DateOnly OrderDate { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
