using System;
using System.Linq;
using System.Web.Mvc;
using QuanLyLuuTru.Models;

namespace QuanLyLuuTru.Controllers
{
    /// <summary>
    /// Phân tách rõ vai trò:
    /// - Quản trị viên (Admin): Quản lý danh mục dịch vụ (Thêm, Sửa, Xóa dịch vụ) tại /Service/Index
    /// - Nhân viên (Staff): Xác nhận dịch vụ khách hàng đã đặt tại /Service/Requests
    /// </summary>
    public class ServiceController : Controller
    {
        // Quản lý dịch vụ (Quản trị viên)
        public ActionResult Index(string category)
        {
            if (AuthHelper.IsStaff())
            {
                return RedirectToAction("Requests");
            }
            if (!AuthHelper.IsAdmin())
            {
                TempData["Error"] = "Chức năng Quản lý dịch vụ (Thêm/Sửa/Xóa) chỉ dành cho Quản trị viên.";
                return RedirectToAction("Login", "Account");
            }

            var db = DataStore.Db;
            var services = db.Services.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                services = services.Where(s => s.Category == category);
            }

            ViewBag.SelectedCategory = category;
            ViewBag.Categories = db.Services.Select(s => s.Category).Distinct().ToList();
            ViewBag.IsAdmin = true;

            ViewBag.UsageCounts = db.BookingServices
                .Where(bs => bs.Status == "Confirmed" || string.IsNullOrEmpty(bs.Status))
                .GroupBy(bs => bs.ServiceId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            return View(services.OrderBy(s => s.Category).ThenBy(s => s.Name).ToList());
        }

        public ActionResult Create()
        {
            if (!AuthHelper.IsAdmin())
            {
                TempData["Error"] = "Chỉ Quản trị viên mới có quyền thêm dịch vụ mới.";
                return RedirectToAction("Index");
            }
            return View(new ServiceItem { Category = "Đồ uống", Unit = "Lon", Price = 20000, IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ServiceItem model)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("Index");

            var db = DataStore.Db;
            if (ModelState.IsValid)
            {
                model.Id = db.Services.Any() ? db.Services.Max(s => s.Id) + 1 : 1;
                db.Services.Add(model);
                DataStore.SaveChanges();
                TempData["Success"] = "Đã thêm dịch vụ '" + model.Name + "' thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            if (!AuthHelper.IsAdmin())
            {
                TempData["Error"] = "Chỉ Quản trị viên mới có quyền sửa dịch vụ.";
                return RedirectToAction("Index");
            }
            var item = DataStore.Db.Services.FirstOrDefault(s => s.Id == id);
            if (item == null) return HttpNotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ServiceItem model)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("Index");

            var db = DataStore.Db;
            var item = db.Services.FirstOrDefault(s => s.Id == model.Id);
            if (item == null) return HttpNotFound();

            if (ModelState.IsValid)
            {
                item.Name = model.Name;
                item.Category = model.Category;
                item.Unit = model.Unit;
                item.Price = model.Price;
                item.IsActive = model.IsActive;

                DataStore.SaveChanges();
                TempData["Success"] = "Đã cập nhật dịch vụ '" + item.Name + "'!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (!AuthHelper.IsAdmin())
            {
                TempData["Error"] = "Chỉ Quản trị viên mới có quyền xóa dịch vụ.";
                return RedirectToAction("Index");
            }

            var db = DataStore.Db;
            var item = db.Services.FirstOrDefault(s => s.Id == id);
            if (item != null)
            {
                db.Services.Remove(item);
                DataStore.SaveChanges();
                TempData["Success"] = "Đã xóa dịch vụ '" + item.Name + "'.";
            }
            return RedirectToAction("Index");
        }

        // Xác nhận dịch vụ khách hàng đặt (Nhân viên)
        public ActionResult Requests(string status)
        {
            if (!AuthHelper.IsStaffOrAdmin())
            {
                TempData["Error"] = "Vui lòng đăng nhập tài khoản Nhân viên để xác nhận dịch vụ.";
                return RedirectToAction("Login", "Account");
            }

            var db = DataStore.Db;
            var query = db.BookingServices.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(bs => bs.Status == status);
            }

            var list = query
                .OrderBy(bs => bs.Status == "Pending" ? 0 : 1)
                .ThenByDescending(bs => bs.Id)
                .ToList();

            ViewBag.SelectedStatus = status;
            ViewBag.PendingCount = db.BookingServices.Count(bs => bs.Status == "Pending");
            ViewBag.BookingDict = db.Bookings.ToDictionary(b => b.Id, b => b);
            ViewBag.RoomDict = db.Rooms.ToDictionary(r => r.Id, r => r);

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmRequest(int id, string returnUrl = null)
        {
            if (!AuthHelper.IsStaffOrAdmin()) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var item = db.BookingServices.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                item.Status = "Confirmed";
                DataStore.RecalculateBookingServiceTotal(item.BookingId);
                TempData["Success"] = string.Format("Đã xác nhận phục vụ dịch vụ '{0}' (SL: {1})!", item.ServiceName, item.Quantity);
            }

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Requests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectRequest(int id, string returnUrl = null)
        {
            if (!AuthHelper.IsStaffOrAdmin()) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var item = db.BookingServices.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                item.Status = "Rejected";
                DataStore.RecalculateBookingServiceTotal(item.BookingId);
                TempData["Success"] = string.Format("Đã báo hết hàng cho dịch vụ '{0}'.", item.ServiceName);
            }

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Requests");
        }
    }
}
