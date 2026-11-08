using System;
using System.Linq;
using System.Web.Mvc;
using QuanLyLuuTru.Models;

namespace QuanLyLuuTru.Controllers
{
    /// <summary>
    /// Quản lý Đăng nhập, Đăng ký, Thông tin cá nhân (màn hình riêng), Đổi mật khẩu (màn hình riêng) và Quản lý tài khoản
    /// </summary>
    public class AccountController : Controller
    {
        // Đăng nhập
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                return View();
            }

            var db = DataStore.Db;
            var user = db.Users.FirstOrDefault(u =>
                u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            if (!user.IsActive)
            {
                ViewBag.Error = "Tài khoản này đang bị khóa. Vui lòng liên hệ Quản lý.";
                return View();
            }

            AuthHelper.SetCurrentUser(user);
            return RedirectToAction("Index", "Home");
        }

        public ActionResult QuickLogin(string username)
        {
            var user = DataStore.Db.Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user != null && user.IsActive)
            {
                AuthHelper.SetCurrentUser(user);
                return RedirectToAction("Index", "Home");
            }
            TempData["Error"] = "Không tìm thấy tài khoản.";
            return RedirectToAction("Login");
        }

        // Đăng ký tài khoản khách hàng
        public ActionResult Register()
        {
            return View(new UserAccount());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(UserAccount model, string ConfirmPassword)
        {
            var db = DataStore.Db;

            if (string.IsNullOrWhiteSpace(model.Username))
            {
                ModelState.AddModelError("Username", "Vui lòng nhập tên đăng nhập.");
            }
            else if (db.Users.Any(u => u.Username.Equals(model.Username.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng.");
            }

            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 3)
            {
                ModelState.AddModelError("Password", "Mật khẩu phải có ít nhất 3 ký tự.");
            }
            else if (model.Password != ConfirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
            }

            if (ModelState.IsValid)
            {
                int nextId = db.Users.Any() ? db.Users.Max(u => u.Id) + 1 : 1;
                model.Id = nextId;
                model.Username = model.Username.Trim();
                model.Role = "Customer";
                model.IsActive = true;
                model.CreatedAt = DateTime.Now;

                db.Users.Add(model);
                DataStore.SaveChanges();

                AuthHelper.SetCurrentUser(model);
                TempData["Success"] = "Đăng ký tài khoản thành công! Xin chào " + model.FullName + ".";
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // Đăng xuất
        public ActionResult Logout()
        {
            AuthHelper.SetCurrentUser(null);
            return RedirectToAction("Login");
        }

        // Quản lý thông tin cá nhân
        public new ActionResult Profile()
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem thông tin cá nhân.";
                return RedirectToAction("Login");
            }
            return View(currentUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public new ActionResult Profile(string FullName, string Phone, string Email, string IdentityCard)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login");

            var userInDb = DataStore.Db.Users.FirstOrDefault(u => u.Id == currentUser.Id);
            if (userInDb == null) return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Phone))
            {
                TempData["Error"] = "Họ tên và số điện thoại không được để trống.";
                return View(userInDb);
            }

            userInDb.FullName = FullName.Trim();
            userInDb.Phone = Phone.Trim();
            userInDb.Email = Email?.Trim();
            userInDb.IdentityCard = IdentityCard?.Trim();

            DataStore.SaveChanges();
            AuthHelper.SetCurrentUser(userInDb);

            TempData["Success"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction("Profile");
        }

        // Đổi mật khẩu
        public ActionResult ChangePassword()
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để đổi mật khẩu.";
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login");

            var userInDb = DataStore.Db.Users.FirstOrDefault(u => u.Id == currentUser.Id);
            if (userInDb == null) return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ mật khẩu hiện tại và mật khẩu mới.";
                return View();
            }

            if (userInDb.Password != CurrentPassword)
            {
                ViewBag.Error = "Mật khẩu hiện tại không chính xác.";
                return View();
            }

            if (NewPassword.Length < 3)
            {
                ViewBag.Error = "Mật khẩu mới phải có ít nhất 3 ký tự.";
                return View();
            }

            if (NewPassword != ConfirmNewPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp với mật khẩu mới.";
                return View();
            }

            userInDb.Password = NewPassword;
            DataStore.SaveChanges();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        // Quản lý tài khoản và phân quyền (Quản trị viên)
        public ActionResult Users(string roleFilter, string keyword)
        {
            if (!AuthHelper.IsAdmin())
            {
                TempData["Error"] = "Chức năng Quản lý tài khoản chỉ dành cho Quản lý.";
                return RedirectToAction("Index", "Home");
            }

            var query = DataStore.Db.Users.AsQueryable();
            if (!string.IsNullOrEmpty(roleFilter))
            {
                query = query.Where(u => u.Role == roleFilter);
            }
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(u =>
                    (u.Username != null && u.Username.ToLower().Contains(kw)) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(kw)) ||
                    (u.Phone != null && u.Phone.ToLower().Contains(kw)));
            }

            ViewBag.SelectedRole = roleFilter;
            ViewBag.Keyword = keyword;
            return View(query.OrderBy(u => u.Id).ToList());
        }

        public ActionResult CreateUser()
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("Index", "Home");
            return View(new UserAccount { Role = "Staff", IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(UserAccount model)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("Index", "Home");

            var db = DataStore.Db;
            if (db.Users.Any(u => u.Username.Equals(model.Username.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                model.Id = db.Users.Any() ? db.Users.Max(u => u.Id) + 1 : 1;
                model.Username = model.Username.Trim();
                model.CreatedAt = DateTime.Now;
                db.Users.Add(model);
                DataStore.SaveChanges();

                TempData["Success"] = "Đã thêm tài khoản '" + model.Username + "' (" + model.RoleText + ") thành công!";
                return RedirectToAction("Users");
            }
            return View(model);
        }

        public ActionResult EditUser(int id)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("Index", "Home");

            var user = DataStore.Db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return HttpNotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(UserAccount model)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("Index", "Home");

            var db = DataStore.Db;
            var user = db.Users.FirstOrDefault(u => u.Id == model.Id);
            if (user == null) return HttpNotFound();

            var current = AuthHelper.GetCurrentUser();
            if (current != null && current.Id == user.Id && model.Role != "Admin")
            {
                ModelState.AddModelError("Role", "Bạn không thể tự hạ quyền Quản lý của tài khoản đang đăng nhập.");
            }

            if (db.Users.Any(u => u.Id != model.Id && u.Username.Equals(model.Username.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập bị trùng với tài khoản khác.");
            }

            if (ModelState.IsValid)
            {
                user.Username = model.Username.Trim();
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    user.Password = model.Password;
                }
                user.FullName = model.FullName;
                user.Phone = model.Phone;
                user.Email = model.Email;
                user.IdentityCard = model.IdentityCard;
                user.Role = model.Role;
                user.IsActive = model.IsActive;

                DataStore.SaveChanges();
                TempData["Success"] = "Đã cập nhật tài khoản '" + user.Username + "' thành công!";
                return RedirectToAction("Users");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleLock(int id)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("Index", "Home");

            var current = AuthHelper.GetCurrentUser();
            var user = DataStore.Db.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                if (current != null && current.Id == user.Id)
                {
                    TempData["Error"] = "Không thể tự khóa tài khoản đang đăng nhập.";
                    return RedirectToAction("Users");
                }
                user.IsActive = !user.IsActive;
                DataStore.SaveChanges();
                TempData["Success"] = user.IsActive
                    ? "Đã mở khóa tài khoản '" + user.Username + "'."
                    : "Đã khóa tài khoản '" + user.Username + "'.";
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("Index", "Home");

            var current = AuthHelper.GetCurrentUser();
            var user = DataStore.Db.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                if (current != null && current.Id == user.Id)
                {
                    TempData["Error"] = "Không thể xóa tài khoản đang đăng nhập.";
                    return RedirectToAction("Users");
                }
                DataStore.Db.Users.Remove(user);
                DataStore.SaveChanges();
                TempData["Success"] = "Đã xóa tài khoản '" + user.Username + "'.";
            }
            return RedirectToAction("Users");
        }
    }
}
