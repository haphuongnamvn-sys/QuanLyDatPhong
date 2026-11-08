using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyLuuTru.Models
{
    /// <summary>
    /// Tài khoản người dùng & Phân quyền (Admin: Quản lý, Staff: Nhân viên, Customer: Khách hàng)
    /// </summary>
    public class UserAccount
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Số CCCD/CMND")]
        public string IdentityCard { get; set; }

        /// <summary>
        /// Quyền hạn: Admin (Quản trị viên), Staff (Nhân viên), Customer (Khách hàng)
        /// </summary>
        [Display(Name = "Vai trò / Phân quyền")]
        public string Role { get; set; }

        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string RoleText
        {
            get
            {
                switch (Role)
                {
                    case "Admin": return "Quản trị viên";
                    case "Staff": return "Nhân viên";
                    case "Customer": return "Khách hàng";
                    default: return Role;
                }
            }
        }

        public string RoleBadgeClass
        {
            get
            {
                switch (Role)
                {
                    case "Admin": return "badge-occupied";
                    case "Staff": return "badge-booked";
                    case "Customer": return "badge-available";
                    default: return "bg-secondary";
                }
            }
        }
    }
}
