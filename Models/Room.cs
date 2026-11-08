using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyLuuTru.Models
{
    /// <summary>
    /// Thông tin phòng và bảng giá lưu trú (Gộp thông tin loại phòng & giá trực tiếp vào phòng để dễ quản lý)
    /// </summary>
    public class Room
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số phòng")]
        [Display(Name = "Số phòng")]
        public string RoomNumber { get; set; }

        [Display(Name = "Tầng")]
        public int Floor { get; set; }

        public int RoomTypeId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục loại phòng")]
        [Display(Name = "Loại phòng")]
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá phòng")]
        [Display(Name = "Đơn giá (VNĐ/đêm)")]
        public decimal PricePerNight { get; set; }

        [Display(Name = "Sức chứa (khách)")]
        public int MaxGuests { get; set; } = 2;

        [Display(Name = "Tiện nghi & Mô tả")]
        public string Description { get; set; }

        /// <summary>
        /// Trạng thái hiện tại: Available (Trống), Occupied (Đang ở), Cleaning (Cần dọn), Maintenance (Bảo trì)
        /// </summary>
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        [Display(Name = "Ghi chú")]
        public string Notes { get; set; }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case "Available": return "Phòng trống";
                    case "Occupied": return "Đã có khách";
                    case "Cleaning": return "Đang dọn phòng";
                    case "Maintenance": return "Đang bảo trì";
                    default: return Status;
                }
            }
        }

        public string StatusBadgeClass
        {
            get
            {
                switch (Status)
                {
                    case "Available": return "badge-available";
                    case "Occupied": return "badge-occupied";
                    case "Cleaning": return "badge-cleaning";
                    case "Maintenance": return "badge-maintenance";
                    default: return "bg-secondary";
                }
            }
        }
    }
}
