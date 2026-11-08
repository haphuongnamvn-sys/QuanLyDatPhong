using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyLuuTru.Models
{
    /// <summary>
    /// Danh mục dịch vụ phát sinh (đồ uống, ăn sáng, giặt ủi, thuê xe, phụ thu...)
    /// </summary>
    public class ServiceItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ")]
        [Display(Name = "Tên dịch vụ")]
        public string Name { get; set; }

        [Display(Name = "Nhóm dịch vụ")]
        public string Category { get; set; }

        [Display(Name = "Đơn vị tính")]
        public string Unit { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn giá")]
        [Display(Name = "Đơn giá (VNĐ)")]
        public decimal Price { get; set; }

        [Display(Name = "Đang phục vụ")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Chi tiết dịch vụ phát sinh khách đã đặt trong kỳ lưu trú
    /// Status: Pending (Chờ nhân viên xác nhận), Confirmed (Đã xác nhận), Rejected (Đã từ chối)
    /// </summary>
    public class BookingService
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string Unit { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Note { get; set; }
        public string Status { get; set; } = "Confirmed";

        public decimal TotalAmount
        {
            get { return Quantity * UnitPrice; }
        }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case "Pending": return "Chờ xác nhận";
                    case "Confirmed": return "Đã xác nhận";
                    case "Rejected": return "Hết hàng";
                    default: return "Đã xác nhận";
                }
            }
        }

        public string StatusBadgeClass
        {
            get
            {
                switch (Status)
                {
                    case "Pending": return "badge-cleaning";
                    case "Confirmed": return "badge-available";
                    case "Rejected": return "badge-cancelled";
                    default: return "badge-available";
                }
            }
        }
    }
}
