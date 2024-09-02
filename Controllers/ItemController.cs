using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Web.Helpers;

namespace Ecommerce.Controllers
{
    
    public class ItemController : Controller
    {
        private readonly EcommerceContext _dbcontext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ItemController(EcommerceContext dbcontext, IWebHostEnvironment webHostEnvironment)
        {
            _dbcontext = dbcontext;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var items = _dbcontext.Items.ToList();
            return View(items);
        }
        [HttpPost]
        public IActionResult Buy(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var cartItem = new CartItem
            {
                CartId = userId,
                ItemId = id,
            };
            _dbcontext.CartItems.Add(cartItem);
            _dbcontext.SaveChanges();
           return RedirectToAction("Index","Checkout");
        }
        [HttpGet]
        public IActionResult Details(int id)
        {
            var item = _dbcontext.Items.FirstOrDefault(x => x.Id == id);
            var rateItem = _dbcontext.RateItems.Where(x => x.ItemId == id).GroupBy(x => x.ItemId).Select(x => new
            {
                Rate = x.Average(r => r.Rating)
            }).FirstOrDefault();
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var cartItemsNumber = _dbcontext.CartItems.Where(x => x.CartId == userId).Count();
            var isAddedToCart = _dbcontext.CartItems.Any(x => x.ItemId == id && x.CartId == userId);
            ViewBag.RateItem = rateItem != null ? rateItem.Rate : 0;
            ViewBag.IsAddedToCart = isAddedToCart;
            ViewBag.Item = id;
            ViewBag.CartItemsNumber = cartItemsNumber;
            if (item == null)
            {
                return NotFound();
            }
            var browsingHistoryOfItem = _dbcontext.BrowsingHistories
                .FirstOrDefault(x => x.ItemId == id && x.UserId == userId); // Assuming `ItemId` is the correct property

            if (browsingHistoryOfItem != null)
            {
                // Update the existing record
                browsingHistoryOfItem.Timestamp = DateTime.UtcNow;
                _dbcontext.BrowsingHistories.Update(browsingHistoryOfItem);
            }
            else
            {
                // Add a new record
                browsingHistoryOfItem = new BrowsingHistory
                {
                    ItemId = id,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow
                };
                _dbcontext.BrowsingHistories.Add(browsingHistoryOfItem);
            }

            // Save changes once, regardless of whether it's an update or insert
            _dbcontext.SaveChanges();


            return View(item);
        }

        [HttpPost]
        public JsonResult AddToCartOrRemove(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value); // Dynamic UserId retrieval
            var cartItem = _dbcontext.CartItems.FirstOrDefault(x => x.ItemId == id && x.CartId == userId);
            bool isAdded = false;

            if (cartItem == null)
            {
                var newCartItem = new CartItem
                {
                    CartId = userId,
                    ItemId = id,
                };
                _dbcontext.CartItems.Add(newCartItem);
                isAdded = true;
            }
            else
            {
                _dbcontext.CartItems.Remove(cartItem);
            }

            _dbcontext.SaveChanges();

            return Json(new { success = true, isAdded = isAdded});
        }
        [HttpPost]
        public JsonResult RemoveFromCart(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value); // Dynamic UserId retrieval
            var cartItem = _dbcontext.CartItems.FirstOrDefault(x => x.ItemId == id && x.CartId == userId);

            if (cartItem != null)
            {
                _dbcontext.CartItems.Remove(cartItem);
                _dbcontext.SaveChanges();
            }

            // Calculate updated total price
            var totalPrice = (from i in _dbcontext.Items
                              join c in _dbcontext.CartItems
                              on i.Id equals c.ItemId
                              where userId == c.CartId
                              select i.Price).Sum();

            // Return JSON result with success flag and updated total price
            return Json(new { success = true, totalPrice = totalPrice });
        }

        public JsonResult GetTotalPrice()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var totalPrice = (from i in _dbcontext.Items
                              join c in _dbcontext.CartItems
                              on i.Id equals c.ItemId
                              where userId == c.CartId
                              select i.Price).Sum();

            return Json(new { totalPrice = totalPrice });
        }



        [HttpPost]
        public JsonResult DeleteItemBrowsingHistory(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var item = _dbcontext.BrowsingHistories.FirstOrDefault(x => x.ItemId == id && x.UserId == userId);

            if (item == null)
            {
                return Json(new { success = false });
            }

            _dbcontext.BrowsingHistories.Remove(item);
            _dbcontext.SaveChanges();

            return Json(new { success = true });
        }
        public IActionResult Create()
        {
            return View();
        }

        // POST: Items/Create
        // POST: Items/Create
        [HttpPost]
        public IActionResult Create(Item item, IFormFile ImageUpload)
        {
            if (ModelState.IsValid)
            {
                // Save the item to get the ID
                _dbcontext.Items.Add(item);
                _dbcontext.SaveChanges();
                // Handle the image upload
                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    // Create the file name as "ItemId_ItemName.ext"
                    var fileExtension = Path.GetExtension(ImageUpload.FileName);
                    var fileName = $"{item.Id}_{item.Name}{fileExtension}";

                    // Set the image path
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);

                    // Save the file
                    using (var fileStream = new FileStream(imagePath, FileMode.Create))
                    {
                        ImageUpload.CopyTo(fileStream);
                    }

                    // Optionally, save the image path to the database if you have a column for it
                    //item.ImagePath = fileName;
                    _dbcontext.Items.Update(item);
                    _dbcontext.SaveChanges();
                }

                return RedirectToAction(nameof(Index));
            }

            // If ModelState is not valid, return the view with the current item to display validation errors
            return View(item);
        }

        // GET: Items/Edit/5
        public IActionResult Edit(int id)
        {
            var item = _dbcontext.Items.Find(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Items/Edit/5
        [HttpPost]
        public IActionResult Edit(int id, Item item, IFormFile ImageUpload)
        {
            if (id != item.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                var existingItem = _dbcontext.Items.Find(id);
                if (existingItem == null)
                {
                    return NotFound();
                }

                // Update item details
                existingItem.Name = item.Name;
                existingItem.Description = item.Description;
                existingItem.Price = item.Price;
                existingItem.ProductId = item.ProductId;

                // Handle the image upload if a new image is provided
                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    // Create the file name as "ItemId_ItemName.ext"
                    var fileExtension = Path.GetExtension(ImageUpload.FileName);
                    var fileName = $"{item.Id}_{item.Name}{fileExtension}";

                    // Set the image path
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);

                    // Delete the old image if it exists
                    var oldImagePattern = $"{item.Id}_*";
                    var oldImageFiles = Directory.GetFiles(Path.Combine(_webHostEnvironment.WebRootPath, "images"), oldImagePattern);
                    foreach (var oldImageFile in oldImageFiles)
                    {
                        if (System.IO.File.Exists(oldImageFile))
                        {
                            System.IO.File.Delete(oldImageFile);
                        }
                    }

                    // Save the new image
                    using (var fileStream = new FileStream(imagePath, FileMode.Create))
                    {
                        ImageUpload.CopyTo(fileStream);
                    }
                }

                _dbcontext.Items.Update(existingItem);
                _dbcontext.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }



        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _dbcontext.Items.Find(id);
            if (item == null)
            {
                return NotFound();
            }

            // Delete the associated image(s) from the server
            var imagePattern = $"{id}_*";
            var imageFiles = Directory.GetFiles(Path.Combine(_webHostEnvironment.WebRootPath, "images"), imagePattern);
            foreach (var imageFile in imageFiles)
            {
                if (System.IO.File.Exists(imageFile))
                {
                    System.IO.File.Delete(imageFile);
                }
            }

            _dbcontext.Items.Remove(item);
            _dbcontext.SaveChanges();
            return Json(new { success = true });
        }


    }
}
