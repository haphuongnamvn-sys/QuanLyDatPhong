using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyLuuTru.Models
{
    /// <summary>
    /// Công việc vệ sinh buồng phòng (Housekeeping)
    /// </summary>
    public class HousekeepingTask
    {
        public int Id { get; set; }

        [Display(Name = "Phòng")]
        public int RoomId { get; set; }

        [Display(Name = "Nhân viên phụ trách")]
        public string StaffName { get; set; }

        [Display(Name = "Loại công việc")]
        public string TaskType { get; set; }

        [Display(Name = "Mức độ ưu tiên")]
        public string Priority { get; set; }

        /// <summary>
        /// Trạng thái: Pending (Chờ dọn), InProgress (Đang dọn), Completed (Đã hoàn thành)
        /// </summary>
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        [Display(Name = "Ghi chú vệ sinh")]
        public string Notes { get; set; }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case "Pending": return "Chờ dọn phòng";
                    case "InProgress": return "Đang dọn dẹp";
                    case "Completed": return "Đã sạch sẽ";
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
                    case "Pending": return "badge-cleaning";
                    case "InProgress": return "badge-booked";
                    case "Completed": return "badge-available";
                    default: return "bg-secondary";
                }
            }
        }
    }
}
