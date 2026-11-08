using System;
using System.Collections.Generic;

namespace QuanLyLuuTru.Models
{
    /// <summary>
    /// Kết quả kiểm tra tình trạng từng phòng theo khoảng thời gian
    /// </summary>
    public class RoomAvailabilityResult
    {
        public Room Room { get; set; }
        public RoomType RoomType { get; set; }
        public bool IsAvailable { get; set; }
        public string ConflictReason { get; set; }
        public Booking ConflictingBooking { get; set; }
        public int Nights { get; set; }
        public decimal EstimatedRoomCharge { get; set; }
    }

    /// <summary>
    /// Thống kê doanh thu & tỷ lệ lấp đầy theo từng hạng phòng
    /// </summary>
    public class RoomTypeReportItem
    {
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; }
        public int RoomCount { get; set; }
        public int TotalAvailableRoomNights { get; set; }
        public int OccupiedRoomNights { get; set; }
        public double OccupancyRate { get; set; }
        public int BookingCount { get; set; }
        public decimal RoomRevenue { get; set; }
        public decimal ServiceRevenue { get; set; }
        public decimal TotalRevenue => RoomRevenue + ServiceRevenue;
    }

    /// <summary>
    /// Thống kê tỷ lệ lấp đầy và doanh thu theo từng phòng cụ thể
    /// </summary>
    public class RoomOccupancyItem
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public string RoomTypeName { get; set; }
        public string CurrentStatusText { get; set; }
        public int TotalDaysInPeriod { get; set; }
        public int OccupiedNights { get; set; }
        public double OccupancyRate { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Thống kê dịch vụ phát sinh bán chạy
    /// </summary>
    public class ServiceRevenueItem
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// ViewModel tổng hợp cho trang Báo cáo Doanh thu & Lấp đầy
    /// </summary>
    public class BusinessReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalDays { get; set; }
        public int TotalRooms { get; set; }
        public int TotalCapacityNights { get; set; }
        public int TotalOccupiedNights { get; set; }
        public double OverallOccupancyRate { get; set; }

        public int TotalBookings { get; set; }
        public int CompletedOrActiveBookings { get; set; }
        public int CancelledBookings { get; set; }

        public decimal TotalRoomRevenue { get; set; }
        public decimal TotalServiceRevenue { get; set; }
        public decimal TotalExtraAndDiscountNet { get; set; }
        public decimal GrandTotalRevenue { get; set; }

        public List<RoomTypeReportItem> RoomTypeReports { get; set; } = new List<RoomTypeReportItem>();
        public List<RoomOccupancyItem> RoomOccupancies { get; set; } = new List<RoomOccupancyItem>();
        public List<ServiceRevenueItem> TopServices { get; set; } = new List<ServiceRevenueItem>();
        public List<Booking> BookingsInPeriod { get; set; } = new List<Booking>();
    }
}
