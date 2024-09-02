using Ecommerce.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ecommerce.Controllers
{
    public class UserController: Controller
    {
        private readonly EcommerceContext _dbcontext;
        public UserController(EcommerceContext dbcontext)
        {
            _dbcontext = dbcontext;
  
        }
        [Authorize(Policy = "User")]

        public IActionResult Index()
        {
            var departments = _dbcontext.Departments.Take(3).ToList();
            var featuredItems = _dbcontext.Items.OrderByDescending(x => x.Id).Take(3).ToList();
            var averageRatings = _dbcontext.RateItems
               .GroupBy(r => r.ItemId)
               .Select(g => new
               {
                   ItemId = g.Key,
                   AverageRating = g.Average(r => r.Rating)
               })
               .ToDictionary(r => r.ItemId, r => r.AverageRating);
            var userId =  int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);



            var itemTreind = (from i in _dbcontext.Items
                             join or in _dbcontext.OrderItems
                             on i.Id equals or.ItemId
                             select i ).Distinct().ToList();
            var cartItemsNumber = _dbcontext.CartItems.Where(x => x.CartId == userId).Count();
            var isInCart = (from ci in _dbcontext.CartItems
                            join i in _dbcontext.Items
                            on ci.ItemId equals i.Id 
                            where ci.CartId == userId
                            select i.Id).ToList();
            ViewBag.InCart = isInCart;
            ViewBag.MostItemsWantIt = itemTreind;
            ViewBag.AverageRatings = averageRatings;
            ViewBag.featuredItems = featuredItems;
            ViewBag.CartItemsNumber = cartItemsNumber;
            return View(departments);
        }
        [Authorize(Policy = "User")]
        public IActionResult GotoSearch()
        {
            return RedirectToAction("Index","Items");
        }
        [Authorize(Policy = "User")]
        [HttpGet]
        public IActionResult FeturedItems()
        {
            return View();
        }
        [HttpGet]
        public JsonResult Departments()
        {
            var departments = _dbcontext.Departments.ToList();
            return Json(departments);
        }
        [HttpGet]
        public JsonResult Products()
        {
            var departments = _dbcontext.Products.ToList();
            return Json(departments);
        }
        [HttpGet]
        public JsonResult GetProductCounts()
        {
            var productCounts = _dbcontext.Departments
                .Select(d => new
                {
                    DepartmentId = d.Id,
                    ProductCount = d.Products.Count()
                }).ToList();

            return Json(productCounts);
        }

        // Query to get the total number of items in each product
        [HttpGet]
        public JsonResult GetItemCounts()
        {
            var itemCounts = _dbcontext.Products
                .Select(p => new
                {
                    ProductId = p.Id,
                    ItemCount = p.Items.Count()
                }).ToList();

            return Json(itemCounts);
        }
     
        public IActionResult Login()
        {
            return View();
        }
    
        [HttpPost]

        public async Task<IActionResult> Login(Login login)
        {
            if (ModelState.IsValid)
            {


                var user = _dbcontext.Users.FirstOrDefault(x => x.Username == login.UsernameOrEmail || x.Email == login.UsernameOrEmail);
                if (user != null)
                {

                    var passworden = PasswordEncryption(login.Password);
                    if (user.Password != passworden)
                    {
                        user = null;
                    }
                }


                var admin = user == null ? _dbcontext.Admins.FirstOrDefault(x =>
                    (x.Username == login.UsernameOrEmail || x.Email == login.UsernameOrEmail)) : null;

                if (admin != null)
                {
                    var password = PasswordEncryption(login.Password);

                    if (admin.Password != password)
                    {
                        admin = null;
                    }
                }
                if (user != null || admin != null)
                {
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, (user?.Id ?? admin.Id).ToString()),
                new Claim(ClaimTypes.Name, user?.Username ?? admin.Username),
                new Claim(ClaimTypes.Email, user?.Email ?? admin.Email),
                new Claim(ClaimTypes.Role, user != null ? "User" : "Admin"),
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties
                        {
                            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                            IsPersistent = true,
                            AllowRefresh = true
                        });

                    return RedirectToAction("Index", user != null ? "User" : "Admin");
                }

                ModelState.AddModelError("", "Username/Email or password not correct");
            }

            return View(login);
        }
       
        public IActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult Register(Register registration)
        {
            if (ModelState.IsValid)
            {
                var checkUserExist = _dbcontext.Users.FirstOrDefault(x => x.Email == registration.Email || x.Username == registration.UserName);
                if (checkUserExist == null)
                {
                    var account = new User
                    {
                        Email = registration.Email,
                        Username = registration.UserName,
                        //Password = registration.Password
                    };
                    var passwordEncryption = PasswordEncryption(registration.Password);
                    account.Password = passwordEncryption;
                    _dbcontext.Users.Add(account);
                    _dbcontext.SaveChanges();
                    var cartOfUsere = new Cart
                    {
                        UserId = account.Id
                    };
                    _dbcontext.Carts.Add(cartOfUsere);
                    _dbcontext.SaveChanges();

                    // Send OTP to the user's email

                    return RedirectToAction("Login");
                }
                if (checkUserExist.Username == registration.UserName)
                {
                    ModelState.AddModelError("", "Username has already been registered.");
                }
                if (checkUserExist.Email == registration.Email)
                {
                    ModelState.AddModelError("", "Email has already been registered.");
                }
            }
            return View(registration);
        }
        public string PasswordEncryption(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty");

            string newPassword = password[0].ToString(); // Start with the first character

            for (int i = 1; i < password.Length - 1; i++)
            {
                char currentChar = password[i];

                if (char.IsDigit(currentChar))
                {
                    // Convert the character to an integer, add one, and convert it back to a character
                    int num = (currentChar - '0' + 1) % 10; // Ensure it's still a digit
                    newPassword += num.ToString();
                }
                else if (char.IsLower(currentChar))
                {
                    // Add 2 if index is even, 1 if index is odd
                    char newChar = (char)(currentChar + (i % 2 == 0 ? 2 : 1));
                    if (newChar > 'z') // Wrap around if it goes beyond 'z'
                        newChar = (char)(newChar - 26);
                    newPassword += newChar;
                }
                else if (char.IsUpper(currentChar))
                {
                    // Add 2 if index is even, 1 if index is odd
                    char newChar = (char)(currentChar + (i % 2 == 0 ? 2 : 1));
                    if (newChar > 'Z') // Wrap around if it goes beyond 'Z'
                        newChar = (char)(newChar - 26);
                    newPassword += newChar;
                }
                else
                {
                    // Keep special characters unchanged
                    newPassword += currentChar;
                }
            }

            newPassword += password[password.Length - 1]; // Append the last character as is
            return newPassword;
        }

        public string PasswordDecryption(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                throw new ArgumentException("Encrypted password cannot be null or empty");

            string originalPassword = encryptedPassword[0].ToString(); // Start with the first character

            for (int i = 1; i < encryptedPassword.Length - 1; i++)
            {
                char currentChar = encryptedPassword[i];

                if (char.IsDigit(currentChar))
                {
                    // Convert the character to an integer, subtract one, and convert it back to a character
                    int num = (currentChar - '0' - 1 + 10) % 10; // Ensure it's still a digit, +10 to handle negative values
                    originalPassword += num.ToString();
                }
                else if (char.IsLower(currentChar))
                {
                    // Subtract 2 if index is even, 1 if index is odd
                    char newChar = (char)(currentChar - (i % 2 == 0 ? 2 : 1));
                    if (newChar < 'a') // Wrap around if it goes below 'a'
                        newChar = (char)(newChar + 26);
                    originalPassword += newChar;
                }
                else if (char.IsUpper(currentChar))
                {
                    // Subtract 2 if index is even, 1 if index is odd
                    char newChar = (char)(currentChar - (i % 2 == 0 ? 2 : 1));
                    if (newChar < 'A') // Wrap around if it goes below 'A'
                        newChar = (char)(newChar + 26);
                    originalPassword += newChar;
                }
                else
                {
                    // Keep special characters unchanged
                    originalPassword += currentChar;
                }
            }

            originalPassword += encryptedPassword[encryptedPassword.Length - 1]; // Append the last character as is
            return originalPassword;
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
        [Authorize(Policy = "User")]
        public IActionResult BrowsingHistory()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var itemInHistory = _dbcontext.BrowsingHistories.Where(x => x.UserId == userId).ToList();

            // Query to get items in history
            var items = from i in _dbcontext.Items
                        join b in _dbcontext.BrowsingHistories
                        on i.Id equals b.ItemId
                        where userId == b.UserId
                        select i;

            var averageRatings = _dbcontext.RateItems
               .GroupBy(r => r.ItemId)
               .Select(g => new
               {
                   ItemId = g.Key,
                   AverageRating = g.Average(r => r.Rating)
               })
               .ToDictionary(r => r.ItemId, r => r.AverageRating);

            var isInCart = (from ci in _dbcontext.CartItems
                            join i in _dbcontext.Items
                            on ci.ItemId equals i.Id
                            where ci.CartId == userId
                            select i.Id).ToList();

            // Pass Items, InCart, and AverageRatings to the view using ViewBag
            ViewBag.Items = items.ToList(); // Convert to list if needed
            ViewBag.InCart = isInCart;
            ViewBag.AverageRatings = averageRatings;
            ViewBag.CartItemsNumber = _dbcontext.CartItems.Count(x => x.CartId == userId);

            return View(itemInHistory);
        }
        [Authorize(Policy = "User")]
        public IActionResult CartItems()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Get items in the user's cart
            var cartItems = (from c in _dbcontext.CartItems
                             join i in _dbcontext.Items
                             on c.ItemId equals i.Id
                             where c.CartId == userId
                             select i).ToList();
            // Get the products associated with the items in the cart
            var productOfCartItems = (from c in cartItems
                                      join p in _dbcontext.Products
                                      on c.ProductId equals p.Id
                                      select p).ToList();

            // Get other items related to the products in the cart
            var otherItems = (from p in _dbcontext.Products
                              join i in _dbcontext.Items
                              on p.Id equals i.ProductId
                              where !cartItems.Select(ci => ci.Id).Contains(i.Id) // Exclude items already in the cart
                              select i).Take(3).ToList();

            var cartItemsNumber = _dbcontext.CartItems.Count(x => x.CartId == userId);
            var totalPrice = cartItems.Sum(i => i.Price);

            ViewBag.TotalPrice = totalPrice;
            // Pass the data to the view using ViewBag
            ViewBag.ProductOfCartItems = productOfCartItems;
            ViewBag.OtherItems = otherItems;
            ViewBag.CartItemsNumber = cartItemsNumber;

            return View(cartItems);
        }

        [Authorize(Policy = "User")]
        public IActionResult OrderItems()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Fetching orders, order items, and tracking orders
            var ordersOfUsers = (from o in _dbcontext.Orders
                                 join oi in _dbcontext.OrderItems
                                 on o.Id equals oi.OrderId
                                 join i in _dbcontext.Items
                                 on oi.ItemId equals i.Id
                                 join to in _dbcontext.TrackingOrders
                                 on new { oi.OrderId, oi.ItemId } equals new { to.OrderId, to.ItemId }
                                 where o.UserId == userId
                                 select new
                                 {
                                     OrderId = o.Id,        // Order Id
                                     ItemId = oi.ItemId,
                                     o.UserId,
                                     Name = i.Name,
                                     Price = i.Price,
                                     Status = to.Status,
                                     Date = o.Date
                                 }).ToList();

            // Convert the query result to a dictionary with OrderId as the key
            var statusOrdersDictionary = ordersOfUsers
                .GroupBy(order => order.OrderId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList() // Group by OrderId and create a list of status and date info
                );

            // Retrieve the items from the database based on the user's orders
            var itemIds = ordersOfUsers.Select(o => o.ItemId).Distinct();
            var items = _dbcontext.Items
                .Where(i => itemIds.Contains(i.Id))
                .ToList();

            // Pass data to the view using ViewBag
            ViewBag.StatusOrdersDictionary = statusOrdersDictionary;
            ViewBag.Items = items;
            ViewBag.OrdersOfUsers = ordersOfUsers;
            return View();
        }

        [Authorize(Policy = "User")]
        public IActionResult Settings()
        {
            return View();
        }
        [HttpPost]
        public JsonResult UpdateEmail(string email)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = _dbcontext.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }
            user.Email = email;
            _dbcontext.Users.Update(user);
            _dbcontext.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateUsername(string username)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = _dbcontext.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }
            user.Username = username;
            _dbcontext.Users.Update(user);
            _dbcontext.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdatePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = _dbcontext.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }
            if (user.Password != PasswordEncryption(oldPassword))
            {
                return Json(new { success = false, message = "Old password is incorrect" });
            }
            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "New passwords do not match" });
            }
            user.Password = PasswordEncryption(newPassword);
            _dbcontext.Users.Update(user);
            _dbcontext.SaveChanges();
            return Json(new { success = true });
        }

    }

}

