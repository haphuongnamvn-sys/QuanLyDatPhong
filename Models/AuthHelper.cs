using System.Linq;
using System.Web;

namespace QuanLyLuuTru.Models
{
    /// <summary>
    /// Lớp hỗ trợ kiểm tra đăng nhập và phân quyền người dùng qua Session
    /// </summary>
    public static class AuthHelper
    {
        public static UserAccount GetCurrentUser()
        {
            if (HttpContext.Current == null || HttpContext.Current.Session == null)
                return null;

            int? userId = HttpContext.Current.Session["UserId"] as int?;
            if (!userId.HasValue)
                return null;

            return DataStore.Db.Users.FirstOrDefault(u => u.Id == userId.Value && u.IsActive);
        }

        public static void SetCurrentUser(UserAccount user)
        {
            if (HttpContext.Current == null || HttpContext.Current.Session == null)
                return;

            if (user == null)
            {
                HttpContext.Current.Session.Clear();
            }
            else
            {
                HttpContext.Current.Session["UserId"] = user.Id;
                HttpContext.Current.Session["Username"] = user.Username;
                HttpContext.Current.Session["FullName"] = user.FullName;
                HttpContext.Current.Session["UserRole"] = user.Role;
            }
        }

        public static bool IsLoggedIn()
        {
            return GetCurrentUser() != null;
        }

        public static string GetRole()
        {
            var user = GetCurrentUser();
            return user != null ? user.Role : "";
        }

        public static bool IsAdmin()
        {
            return GetRole() == "Admin";
        }

        public static bool IsStaff()
        {
            return GetRole() == "Staff";
        }

        public static bool IsStaffOrAdmin()
        {
            string role = GetRole();
            return role == "Admin" || role == "Staff";
        }

        public static bool IsCustomer()
        {
            return GetRole() == "Customer";
        }
    }
}
