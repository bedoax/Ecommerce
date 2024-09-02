using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Transactions;

namespace Ecommerce.Controllers
{
    [Authorize(Policy = "User")]
    public class CheckoutController : Controller
    {
        private readonly EcommerceContext _dbcontext;
        private readonly PaymentService _paymentService;
        private readonly StripeSettings _stripeSettings;

        public CheckoutController(EcommerceContext dbcontext, PaymentService paymentService, IOptions<StripeSettings> stripeSettings)
        {
            _dbcontext = dbcontext;
            _paymentService = paymentService;
            _stripeSettings = stripeSettings.Value;
        }

        public IActionResult Index()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var cartItems = (from c in _dbcontext.CartItems
                                 join i in _dbcontext.Items
                                 on c.ItemId equals i.Id
                                 where c.CartId == userId
                                 select i).ToList();
                var cartItemsNumber = _dbcontext.CartItems.Count(x => x.CartId == userId);
                var totalAmount = cartItems.Sum(item => item.Price);

                // Pass total amount and publishable key to the view
                ViewBag.TotalAmount = totalAmount;
                ViewBag.StripePublishableKey = _stripeSettings.PublishableKey;
                ViewBag.CartItemsNumber = cartItemsNumber;
                return View(cartItems);
            }
            catch (Exception ex)
            {
                // Log the exception
                // Example: _logger.LogError(ex, "Error in Index action of CheckoutController");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var cartItems = (from c in _dbcontext.CartItems
                                 join i in _dbcontext.Items
                                 on c.ItemId equals i.Id
                                 where c.CartId == userId
                                 select i).ToList();

                var totalAmount = cartItems.Sum(item => item.Price);
                var clientSecret = await _paymentService.CreatePaymentIntent(totalAmount);

                return Json(new { clientSecret = clientSecret });
            }
            catch (Exception ex)
            {
                // Log the exception
                // Example: _logger.LogError(ex, "Error in CreatePaymentIntent action of CheckoutController");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public IActionResult Success(string country, string state, string street, string paymentMethodId)
        {
            using (var transaction = _dbcontext.Database.BeginTransaction())
            {
                try
                {
                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                    // Retrieve the cart items
                    var cartItems = (from c in _dbcontext.CartItems
                                     join i in _dbcontext.Items
                                     on c.ItemId equals i.Id
                                     where c.CartId == userId
                                     select i).ToList();

                    if (!cartItems.Any())
                    {
                        return BadRequest("No items in the cart.");
                    }

                    // Create a new order
                    var order = new Order
                    {
                        UserId = userId,
                        Date = DateTime.Now,
                        Amount = (int)cartItems.Sum(item => item.Price)
                    };
                    _dbcontext.Orders.Add(order);
                    _dbcontext.SaveChanges(); // Save the order first to generate OrderId

                    // Save the address
                    var existingAddress = _dbcontext.Addresses.FirstOrDefault(a =>
                        a.Country == country &&
                        a.State == state &&
                        a.Street == street &&
                        a.UserId == userId);

                    if (existingAddress == null)
                    {
                        var address = new Address
                        {
                            Country = country,
                            State = state,
                            Street = street,
                            UserId = userId
                        };
                        _dbcontext.Addresses.Add(address);
                        _dbcontext.SaveChanges();
                    }

                    // Add items to the order and create tracking orders
                    foreach (var item in cartItems)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ItemId = item.Id
                        };
                        _dbcontext.OrderItems.Add(orderItem);
                        _dbcontext.SaveChanges(); // Save to generate OrderItemId

                        var trackingOrder = new TrackingOrder
                        {
                            OrderId = order.Id, // Set the OrderId properly
                            ItemId = item.Id,
                            Status = "Pending"
                        };
                        _dbcontext.TrackingOrders.Add(trackingOrder);
                    }

                    // Save changes to add tracking orders
                    _dbcontext.SaveChanges();

                    // Optionally clear the cart
                    var cartItemsToRemove = _dbcontext.CartItems.Where(c => c.CartId == userId);
                    _dbcontext.CartItems.RemoveRange(cartItemsToRemove);
                    _dbcontext.SaveChanges();

                    // Commit transaction
                    transaction.Commit();

                    // Send a notification (this is a placeholder for your actual notification logic)
                    // SendOrderSuccessNotification(userId, order.Id);

                    // Redirect to Orders page
                    return RedirectToAction("OrderItems", "User");
                }
                catch (Exception ex)
                {
                    // Rollback transaction in case of an error
                    transaction.Rollback();

                    // Log the exception
                    // Example: _logger.LogError(ex, "Error in Success action of CheckoutController");
                    return StatusCode(500, "Internal server error");
                }
            }
        }

        // This is a placeholder method for sending a notification to the user
        private void SendOrderSuccessNotification(int userId, int orderId)
        {
            // Implement your notification logic here
            // Example: send an email or an in-app notification to the user
        }
    }
}
