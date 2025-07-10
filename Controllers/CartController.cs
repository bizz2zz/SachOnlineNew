using LuongVinhKhang.SachOnline.Data;
using LuongVinhKhang.SachOnline.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using LuongVinhKhang.SachOnline.Helpers;


namespace LuongVinhKhang.SachOnline.Controllers
{
    public class CartController : Controller
    {
        private readonly BookstoreContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(BookstoreContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Cart()
        {
            if (User.Identity.IsAuthenticated)
            {
                var taiKhoan = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.TaiKhoan == taiKhoan);
                if (user == null)
                {
                    return NotFound("Không tìm thấy thông tin người dùng.");
                }

                ViewBag.CartCount = await _context.Cart
                    .Where(c => c.MaKH == user.MaKH)
                    .SumAsync(c => (int?)c.Quantity) ?? 0;

                var cartItems = await _context.Cart
                    .Include(c => c.Product).ThenInclude(p => p.ChuDe)
                    .Include(c => c.Product.NhaXuatBan)
                    .Where(c => c.MaKH == user.MaKH)
                    .ToListAsync();

                return View("Cart", cartItems);
            }
            else
            {
                // Người chưa đăng nhập → Lấy giỏ từ Session
                var sessionCart = HttpContext.Session.GetObject<List<CartSessionItem>>("CartSession") ?? new List<CartSessionItem>();

                // Tạo list cart giả để hiển thị
                var cartItems = new List<Cart>();
                foreach (var item in sessionCart)
                {
                    var product = await _context.Product
                        .Include(p => p.ChuDe)
                        .Include(p => p.NhaXuatBan)
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                    if (product != null)
                    {
                        cartItems.Add(new Cart
                        {
                            Product = product,
                            ProductId = product.Id,
                            Quantity = item.Quantity
                        });
                    }
                }

                ViewBag.CartCount = sessionCart.Sum(c => c.Quantity);

                return View("Cart", cartItems); // Dùng chung view Cart.cshtml
            }
        }


        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Product.FindAsync(productId);
            if (product == null)
            {
                return NotFound("Sản phẩm không tồn tại.");
            }

            if (User.Identity.IsAuthenticated)
            {
                // Người dùng đã đăng nhập → thêm vào DB
                var user = await _context.Users.FirstOrDefaultAsync(u => u.TaiKhoan == User.Identity.Name);
                if (user == null)
                {
                    return NotFound("Không tìm thấy thông tin người dùng.");
                }

                var existingCartItem = await _context.Cart
                    .FirstOrDefaultAsync(c => c.MaKH == user.MaKH && c.ProductId == productId);

                int currentCartQuantity = existingCartItem?.Quantity ?? 0;
                int newTotalQuantity = currentCartQuantity + quantity;

                if (newTotalQuantity > product.SoLuong)
                {
                    TempData["ErrorMessage"] = $"Sách \"{product.Name}\" chỉ còn {product.SoLuong} sản phẩm trong kho.";
                    return RedirectToAction("Cart");
                }

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity = newTotalQuantity;
                }
                else
                {
                    _context.Cart.Add(new Cart
                    {
                        MaKH = user.MaKH,
                        ProductId = productId,
                        Quantity = quantity
                    });
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                // Người dùng chưa đăng nhập → lưu giỏ tạm trong session
                var cart = HttpContext.Session.GetObject<List<CartSessionItem>>("CartSession") ?? new List<CartSessionItem>();

                var existing = cart.FirstOrDefault(c => c.ProductId == productId);
                int newQty = (existing?.Quantity ?? 0) + quantity;

                if (newQty > product.SoLuong)
                {
                    TempData["ErrorMessage"] = $"Sách \"{product.Name}\" chỉ còn {product.SoLuong} sản phẩm trong kho.";
                    return RedirectToAction("Cart");
                }

                if (existing != null)
                {
                    existing.Quantity = newQty;
                }
                else
                {
                    cart.Add(new CartSessionItem { ProductId = productId, Quantity = quantity });
                }

                HttpContext.Session.SetObject("CartSession", cart);
            }

            return RedirectToAction("Cart");
        }



        [HttpPost]
        public async Task<IActionResult> UpdateCart(List<CartUpdateViewModel> cartItems)
        {
            bool hasAdjustment = false;

            if (User.Identity.IsAuthenticated)
            {
                // Người dùng đã đăng nhập → Cập nhật giỏ từ DB
                foreach (var item in cartItems)
                {
                    var cartItem = await _context.Cart.FindAsync(item.CartId);
                    if (cartItem != null && item.Quantity > 0)
                    {
                        var product = await _context.Product.FindAsync(cartItem.ProductId);
                        if (product != null)
                        {
                            if (item.Quantity > product.SoLuong)
                            {
                                cartItem.Quantity = product.SoLuong;
                                hasAdjustment = true;
                            }
                            else
                            {
                                cartItem.Quantity = item.Quantity;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                // Người chưa đăng nhập → Cập nhật giỏ từ Session
                var sessionCart = HttpContext.Session.GetObject<List<CartSessionItem>>("CartSession") ?? new List<CartSessionItem>();

                foreach (var item in cartItems)
                {
                    var cartItem = sessionCart.FirstOrDefault(c => c.ProductId == item.ProductId);
                    if (cartItem != null)
                    {
                        var product = await _context.Product.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            if (item.Quantity > product.SoLuong)
                            {
                                cartItem.Quantity = product.SoLuong;
                                hasAdjustment = true;
                            }
                            else
                            {
                                cartItem.Quantity = item.Quantity;
                            }
                        }
                    }
                }

                HttpContext.Session.SetObject("CartSession", sessionCart);
            }

            if (hasAdjustment)
            {
                TempData["ErrorMessage"] = "Một số sản phẩm đã được điều chỉnh về số lượng tồn kho.";
            }

            return RedirectToAction("Cart");
        }




        [HttpPost]
        public IActionResult RemoveFromCart(int cartId, int? productId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var cartItem = _context.Cart.Find(cartId);
                if (cartItem != null)
                {
                    _context.Cart.Remove(cartItem);
                    _context.SaveChanges();
                }
            }
            else
            {
                var cart = HttpContext.Session.GetObject<List<CartSessionItem>>("CartSession") ?? new List<CartSessionItem>();
                var item = cart.FirstOrDefault(c => c.ProductId == productId);
                if (item != null)
                {
                    cart.Remove(item);
                    HttpContext.Session.SetObject("CartSession", cart);
                }
            }

            return RedirectToAction("Cart");
        }

    }
}