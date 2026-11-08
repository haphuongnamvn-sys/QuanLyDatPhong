using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyLuuTru.Models
{
    /// <summary>
    /// Thông tin đơn đặt phòng, nhận phòng, trả phòng và thanh toán
    /// </summary>
    public class Booking
    {
        public int Id { get; set; }

        [Display(Name = "Mã đơn")]
        public string BookingCode { get; set; }

        // Liên kết tài khoản khách hàng (nếu khách đặt online)
        public int? CustomerUserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên khách hàng")]
        [Display(Name = "Họ tên khách")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        public string CustomerPhone { get; set; }

        [Display(Name = "Số CCCD/CMND")]
        public string CustomerIdentityCard { get; set; }

        [Display(Name = "Số khách")]
        public int GuestCount { get; set; }

        [Display(Name = "Phòng")]
        public int RoomId { get; set; }

        [Display(Name = "Ngày nhận phòng")]
        public DateTime CheckInDate { get; set; }

        [Display(Name = "Ngày trả phòng")]
        public DateTime CheckOutDate { get; set; }

        [Display(Name = "Giờ nhận thực tế")]
        public DateTime? ActualCheckIn { get; set; }

        [Display(Name = "Giờ trả thực tế")]
        public DateTime? ActualCheckOut { get; set; }

        [Display(Name = "Giá phòng/đêm")]
        public decimal RoomPricePerNight { get; set; }

        [Display(Name = "Số đêm")]
        public int TotalNights { get; set; }

        [Display(Name = "Tiền phòng")]
        public decimal RoomTotalAmount { get; set; }

        [Display(Name = "Tiền dịch vụ")]
        public decimal ServiceTotalAmount { get; set; }

        [Display(Name = "Phụ thu")]
        public decimal ExtraCharge { get; set; }

        [Display(Name = "Giảm giá")]
        public decimal DiscountAmount { get; set; }

        [Display(Name = "Tiền cọc")]
        public decimal DepositAmount { get; set; }

        /// <summary>
        /// Trạng thái: Booked (Đã đặt), CheckedIn (Đang ở), CheckedOut (Đã trả phòng), Cancelled (Đã hủy)
        /// </summary>
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        [Display(Name = "Ghi chú")]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        // Tổng tiền = Tiền phòng + Dịch vụ + Phụ thu - Giảm giá
        public decimal FinalTotal
        {
            get { return RoomTotalAmount + ServiceTotalAmount + ExtraCharge - DiscountAmount; }
        }

        // Số tiền còn phải thanh toán
        public decimal RemainingAmount
        {
            get { return FinalTotal - DepositAmount; }
        }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case "Booked": return "Chờ nhận phòng";
                    case "CheckedIn": return "Đang ở";
                    case "CheckedOut": return "Đã trả phòng";
                    case "Cancelled": return "Đã hủy";
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
                    case "Booked": return "badge-booked";
                    case "CheckedIn": return "badge-occupied";
                    case "CheckedOut": return "badge-completed";
                    case "Cancelled": return "badge-cancelled";
                    default: return "bg-secondary";
                }
            }
        }
    }
}
