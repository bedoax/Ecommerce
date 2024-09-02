using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Ecommerce.Controllers
{
    [Authorize(Policy = "Admin")]
    public class AdminController : Controller
    {
        private readonly EcommerceContext _dbcontext;

        public AdminController(EcommerceContext dbcontext)
        {
            _dbcontext = dbcontext;
        }
      
        public IActionResult Index()
        {
            var users = _dbcontext.Users.ToList();
            return View(users);
        }
       
        public IActionResult EditUser(int id)
        {
            var user = _dbcontext.Users.Find(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        public IActionResult EditUser(User model)
        {
            var user = _dbcontext.Users.Find(model.Id);

            if (user == null)
            {
                return NotFound();
            }

            // Update user details
            user.Username = model.Username;
            user.Email = model.Email;
            user.Password = model.Password; // Hash password before saving

            _dbcontext.Users.Update(user);
            _dbcontext.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult DeleteUser(int id)
        {
            using (var transaction = _dbcontext.Database.BeginTransaction())
            {
                try
                {
                    var user = _dbcontext.Users.Find(id);

                    if (user == null)
                    {
                        return Json(new { success = false, message = "User not found" });
                    }

                    // Remove related BrowsingHistories
                    var browsingHistories = _dbcontext.BrowsingHistories.Where(b => b.UserId == id).ToList();
                    _dbcontext.BrowsingHistories.RemoveRange(browsingHistories);

                    // Remove related CartItems and Cart
                    var cart = _dbcontext.Carts.FirstOrDefault(c => c.UserId == id);
                    if (cart != null)
                    {
                        var cartItems = _dbcontext.CartItems.Where(ci => ci.CartId == cart.Id).ToList();
                        _dbcontext.CartItems.RemoveRange(cartItems);
                        _dbcontext.Carts.Remove(cart);
                    }

                    // Remove related RateItems
                    var rateItems = _dbcontext.RateItems.Where(ri => ri.UserId == id).ToList();
                    _dbcontext.RateItems.RemoveRange(rateItems);

                    // Remove related OrderItems, Orders, and TrackingOrders
                    var orders = _dbcontext.Orders.Where(o => o.UserId == id).ToList();
                    foreach (var order in orders)
                    {
                        var orderItems = _dbcontext.OrderItems.Where(oi => oi.OrderId == order.Id).ToList();
                        _dbcontext.OrderItems.RemoveRange(orderItems);

                        // Use ItemId directly to find related TrackingOrders
                        var trackingOrders = _dbcontext.TrackingOrders
                            .Where(to => orderItems.Any(oi => oi.ItemId == to.ItemId && oi.OrderId == to.OrderId))
                            .ToList();
                        _dbcontext.TrackingOrders.RemoveRange(trackingOrders);
                    }
                    _dbcontext.Orders.RemoveRange(orders);

                    // Remove related Address
                    var address = _dbcontext.Addresses.FirstOrDefault(a => a.UserId == id);
                    if (address != null)
                    {
                        _dbcontext.Addresses.Remove(address);
                    }

                    // Finally, remove the user
                    _dbcontext.Users.Remove(user);

                    // Save all changes to the database
                    _dbcontext.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true });
                }
                catch
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "An error occurred while deleting the user" });
                }
            }
        }

        public IActionResult Statistics()
        {
            var mostThreeItemsInCart = _dbcontext.CartItems
                .GroupBy(ci => ci.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    CartCount = g.Count(),
                    ItemName = _dbcontext.Items
                        .Where(i => i.Id == g.Key)
                        .Select(i => i.Name)
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.CartCount)
                .Take(3)
                .ToList();

            var mostThreeItemsInOrder = _dbcontext.OrderItems
                .GroupBy(oi => oi.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    OrderCount = g.Count(),
                    ItemName = _dbcontext.Items
                        .Where(i => i.Id == g.Key)
                        .Select(i => i.Name)
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.OrderCount)
                .Take(3)
                .ToList();

            var mostThreeItemsViewed = _dbcontext.BrowsingHistories
                .GroupBy(b => b.ItemId)
                .Select(x => new
                {
                    ItemId = x.Key,
                    BrowsingCount = x.Count(),
                    ItemName = _dbcontext.Items
                        .Where(i => i.Id == x.Key)
                        .Select(i => i.Name)
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.BrowsingCount)
                .Take(3)
                .ToList();

            ViewBag.MostThreeItemsInCart = mostThreeItemsInCart;
            ViewBag.MostThreeItemsInOrder = mostThreeItemsInOrder;
            ViewBag.MostThreeItemsViewed = mostThreeItemsViewed;

            return View();
        }
      
        public IActionResult Orders()
        {
            var orders = (from u in _dbcontext.Users
                          join o in _dbcontext.Orders on u.Id equals o.UserId
                          join oi in _dbcontext.OrderItems on o.Id equals oi.OrderId
                          join i in _dbcontext.Items on oi.ItemId equals i.Id
                          join tro in _dbcontext.TrackingOrders on new { oi.OrderId, oi.ItemId } equals new { tro.OrderId, tro.ItemId }
                          select new 
                          {
                              OrderId = oi.OrderId,
                              UserId = u.Id,
                              Username = u.Username,
                              ItemName = i.Name,
                              ItemPrice = i.Price,
                              OrderAmount = o.Amount,
                              OrderDate = o.Date,
                              OrderStatus = tro.Status
                          }).ToList();

            ViewBag.Orders = orders;
            return View();
        }

        [HttpGet]
        public IActionResult Search(string searchTerm)
        {
            var query = (from u in _dbcontext.Users
                         join o in _dbcontext.Orders on u.Id equals o.UserId
                         join oi in _dbcontext.OrderItems on o.Id equals oi.OrderId
                         join i in _dbcontext.Items on oi.ItemId equals i.Id
                         join tro in _dbcontext.TrackingOrders on new { oi.OrderId, oi.ItemId } equals new { tro.OrderId, tro.ItemId }
                         where u.Username.ToLower().Contains(searchTerm.ToLower()) || u.Id.ToString().Contains(searchTerm)
                         select new 
                         {
                             OrderId = oi.OrderId,
                             UserId = u.Id,
                             Username = u.Username,
                             ItemName = i.Name,
                             ItemPrice = i.Price,
                             OrderAmount = o.Amount,
                             OrderDate = o.Date,
                             OrderStatus = tro.Status
                         }).ToList();

            return Json(query);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int orderId, string itemName, string status)
        {
            var item = _dbcontext.Items.FirstOrDefault(x => x.Name == itemName);
            if (item != null)
            {
                var trackingOrder = _dbcontext.TrackingOrders
                    .FirstOrDefault(tro => tro.OrderId == orderId && tro.ItemId == item.Id);

                if (trackingOrder != null)
                {
                    trackingOrder.Status = status;
                    _dbcontext.SaveChanges();
                    return Json(new { success = true, status = trackingOrder.Status });
                }
            }

            return Json(new { success = false, message = "Order or Item not found" });
        }

    }
}
