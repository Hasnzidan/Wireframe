using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wireframe.Data;
using Wireframe.Models;
using Wireframe.Services;
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

        public ProductController(IProductService productService, IWebHostEnvironment webHostEnvironment)
        {
            _productService = productService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string name, string fromDate, string toDate)
        {
            var products = await _productService.GetProductsAsync(name, 
                !string.IsNullOrEmpty(fromDate) ? DateOnly.Parse(fromDate) : null,
                !string.IsNullOrEmpty(toDate) ? DateOnly.Parse(toDate) : null);
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

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile ImageFile)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(product.Imgurl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.Imgurl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }

                    product.Imgurl = "/uploads/" + uniqueFileName;
                }

                var success = await _productService.UpdateProductAsync(product);
                if (!success)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        [HttpPost]
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
