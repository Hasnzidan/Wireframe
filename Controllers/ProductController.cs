using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wireframe.Data;
using Wireframe.Models;
using Wireframe.Services;
using Wireframe.Helpers;
using System.IO;
using System;
using System.Text.Json;

namespace Wireframe.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;

        public ProductController(IProductService productService, IWebHostEnvironment webHostEnvironment, ApplicationDbContext context)
        {
            _productService = productService;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        public async Task<IActionResult> Index(string name, string fromDate, string toDate, int? pageNumber)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(p => p.Name.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(fromDate) && DateOnly.TryParse(fromDate, out var from))
            {
                query = query.Where(p => p.Date >= from);
            }

            if (!string.IsNullOrWhiteSpace(toDate) && DateOnly.TryParse(toDate, out var to))
            {
                query = query.Where(p => p.Date <= to);
            }

            const int pageSize = 4;
            query = query.OrderByDescending(p => p.Date);
            
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }

            var products = await PaginatedList<Product>.CreateAsync(query, pageNumber ?? 1, pageSize);

            ViewData["CurrentFilter"] = name;
            ViewData["FromDate"] = fromDate;
            ViewData["ToDate"] = toDate;
            ViewData["CurrentPage"] = pageNumber ?? 1;

            return View(products);
        }

        public IActionResult Create()
        {
            return View(new Product { Date = DateOnly.FromDateTime(DateTime.Now) });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile ImageFile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    ModelState.AddModelError("Name", "Product name is required");
                    return View(product);
                }

                if (product.price < 0m)
                {
                    ModelState.AddModelError("price", "Price must be a positive number");
                    return View(product);
                }

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }

                    product.Imgurl = "/uploads/" + uniqueFileName;
                }

                product.Date = DateOnly.FromDateTime(DateTime.Now);
                var createdProduct = await _productService.CreateProductAsync(product);
                
                if (createdProduct != null)
                {
                    TempData["Success"] = "Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create product.";
                    return View(product);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error creating product: " + ex.Message);
                TempData["Error"] = "An error occurred while creating the product.";
                return View(product);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? ImageFile)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                ModelState.AddModelError("Name", "Product name is required");
                return View(product);
            }

            if (product.price < 0m)
            {
                ModelState.AddModelError("price", "Price must be a positive number");
                return View(product);
            }

            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // If a new image is uploaded, process it
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Delete the old image if it exists
                    if (!string.IsNullOrEmpty(existingProduct.Imgurl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProduct.Imgurl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Save the new image
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }

                    product.Imgurl = "/uploads/" + uniqueFileName;
                }
                else
                {
                    // Keep the existing image URL if no new image is uploaded
                    product.Imgurl = existingProduct.Imgurl;
                }

                // Update other properties
                existingProduct.Name = product.Name;
                existingProduct.price = product.price;
                existingProduct.Date = product.Date;
                existingProduct.Imgurl = product.Imgurl;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Products.AnyAsync(e => e.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product != null && !string.IsNullOrEmpty(product.Imgurl))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.Imgurl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            await _productService.DeleteProductAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
