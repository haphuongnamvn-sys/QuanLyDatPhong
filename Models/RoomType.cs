using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyLuuTru.Models
{
    /// <summary>
    /// Bảng giá và thông tin hạng phòng / loại phòng
    /// </summary>
    public class RoomType
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên loại phòng")]
        [Display(Name = "Tên hạng phòng")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá ngày thường")]
        [Display(Name = "Giá ngày thường (VNĐ/đêm)")]
        public decimal BasePrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá cuối tuần")]
        [Display(Name = "Giá cuối tuần T7-CN (VNĐ/đêm)")]
        public decimal WeekendPrice { get; set; }

        [Display(Name = "Phụ thu trả muộn (VNĐ/giờ)")]
        public decimal LateCheckoutFeePerHour { get; set; }

        [Display(Name = "Số khách tối đa")]
        public int MaxGuests { get; set; }

        [Display(Name = "Tiện nghi & Mô tả")]
        public string Description { get; set; }
    }
}
