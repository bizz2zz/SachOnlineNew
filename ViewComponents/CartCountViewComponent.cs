using LuongVinhKhang.SachOnline.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LuongVinhKhang.SachOnline.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly BookstoreContext _context;

        public CartCountViewComponent(BookstoreContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int count = 0;

            if (User.Identity.IsAuthenticated)
            {
                var taiKhoan = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.TaiKhoan == taiKhoan);
                if (user != null)
                {
                    count = await _context.Cart
                        .Where(c => c.MaKH == user.MaKH)
                        .SumAsync(c => (int?)c.Quantity) ?? 0;
                }
            }

            return View("_CartCountPartial", count);
        }
    }
}
