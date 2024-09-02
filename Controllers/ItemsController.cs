using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ecommerce.Controllers
{
    [Authorize(Policy = "User,Admin")]
    public class ItemsController : Controller
    {
        private readonly EcommerceContext _dbcontext;

        public ItemsController(EcommerceContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        public IActionResult Index(string query,string searchContent)
        {
            // Load departments, products, items, and ratings
            var departments = _dbcontext.Departments.ToList();
            var products = _dbcontext.Products.ToList();
            var items = _dbcontext.Items.ToList();
            var rateItems = _dbcontext.RateItems
                .GroupBy(x => x.ItemId)
                .Select(x => new
                {
                    ItemId = x.Key,
                    AverageRating = (int)x.Average(r => r.Rating)
                })
                .ToDictionary(r => r.ItemId, r => r.AverageRating);
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var cartItemsNumber = _dbcontext.CartItems.Where(x => x.CartId == userId).Count();
            ViewBag.Departments = departments;
            ViewBag.Products = products;
            ViewBag.RateItem = rateItems;
            ViewBag.SearchContent = "";
            if(query != null)
            {
                ViewBag.SearchContent = query;
            }
            if(searchContent != null)
            {
                ViewBag.SearchContent = searchContent;
            }
            // change this operation later when u finish the login page and signup page , to show the user who have cart 
            var isInCart = (from ci in _dbcontext.CartItems
                           join i in _dbcontext.Items
                           on ci.ItemId equals i.Id
                           where ci.CartId == userId
                           select i.Id).ToList();
            ViewBag.InCart = isInCart;
            ViewBag.CartItemsNumber = cartItemsNumber;
            return View(items);
        }

        [HttpGet]
        public JsonResult Filter(int? productId , int? minPrice , int? maxPrice)
        {
            // Start with the base query for items
            var query = _dbcontext.Items.AsQueryable();

            // Apply filter if productId is provided
            if (productId.HasValue)
            {
                query = query.Where(i => i.ProductId == productId.Value);

            }
            if (minPrice.HasValue)
            {
                query = query.Where(i => i.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(i => i.Price <= maxPrice.Value);
            }
            // Execute query to get filtered items
            var filteredItems = query.ToList();

            // Get ratings for the filtered items
            var rateItems = _dbcontext.RateItems
                .Where(x => filteredItems.Select(fi => fi.Id).Contains(x.ItemId))
                .GroupBy(x => x.ItemId)
                .Select(x => new
                {
                    ItemId = x.Key,
                    AverageRating = (int)x.Average(r => r.Rating)
                })
                .ToDictionary(r => r.ItemId, r => r.AverageRating);

            // Retrieve cart items for the current user (assuming userId is known)
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var inCart = _dbcontext.CartItems
                .Where(ci => ci.CartId == userId)
                .Select(ci => ci.ItemId)
                .ToList();
            // Return filtered items, ratings, and cart items as JSON
            return Json(new { Items = filteredItems, Ratings = rateItems, InCart = inCart });
        }
        [HttpGet]
        public async Task<JsonResult> AddCartOrRemove(int itemId)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var isItemInCart = await _dbcontext.CartItems.AnyAsync(x => x.ItemId == itemId && x.CartId == userId);

                if (!isItemInCart)
                {
                    var cartItem = new CartItem
                    {
                        CartId = userId,
                        ItemId = itemId,
                    };
                    _dbcontext.CartItems.Add(cartItem);
                }
                else
                {
                    var cartItem = await _dbcontext.CartItems.FirstOrDefaultAsync(x => x.ItemId == itemId && x.CartId == userId);
                    if (cartItem != null)
                    {
                        _dbcontext.CartItems.Remove(cartItem);
                    }
                }

                await _dbcontext.SaveChangesAsync();
                return Json(!isItemInCart); // Return true if item was added, false if removed
            

        }

        [HttpGet]
        public JsonResult SearchItem(string item)
        {
            if(item == "" || item == null)
            {
                var items = _dbcontext.Items.Select(x=> new { x.Id, x.Name, x.Price }).ToList();
                var rateItems2 = _dbcontext.RateItems
                 .Where(x => items.Select(x=>x.Id).Contains(x.ItemId))
                 .GroupBy(x => x.ItemId)
                 .Select(x => new
                 {
                     ItemId = x.Key,
                     AverageRating = (int)x.Average(r => r.Rating)
                 })
                 .ToDictionary(r => r.ItemId, r => r.AverageRating);
                var userId2 = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var inCart2 = _dbcontext.CartItems
                    .Where(ci => ci.CartId == userId2 && items.Select(x => x.Id).Contains(ci.ItemId))
                    .Select(ci => ci.ItemId)
                    .ToList();
                return Json(new { Items = items, Ratings = rateItems2, InCart = inCart2 });
            }
            string searchTerm = item.Trim().ToLower();

            // Ensure indexing on the Name field for better performance
            var getItemQuery = _dbcontext.Items
                .Where(x => x.Name.ToLower().Contains(searchTerm))
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Price // Only select necessary fields
                });

            var getItem = getItemQuery.ToList();

            if (!getItem.Any())
            {
                return Json("Not Found");
            }

            // Get item IDs for rating calculation
            var itemIds = getItem.Select(i => i.Id).ToList();

            var rateItems = _dbcontext.RateItems
                .Where(x => itemIds.Contains(x.ItemId))
                .GroupBy(x => x.ItemId)
                .Select(x => new
                {
                    ItemId = x.Key,
                    AverageRating = (int)x.Average(r => r.Rating)
                })
                .ToDictionary(r => r.ItemId, r => r.AverageRating);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var inCart = _dbcontext.CartItems
                .Where(ci => ci.CartId == userId && itemIds.Contains(ci.ItemId))
                .Select(ci => ci.ItemId)
                .ToList();

            return Json(new { Items = getItem, Ratings = rateItems, InCart = inCart });
        }


    }
}
