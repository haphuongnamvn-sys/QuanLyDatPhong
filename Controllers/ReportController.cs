using System;
using System.Linq;
using System.Web.Mvc;
using QuanLyLuuTru.Models;

namespace QuanLyLuuTru.Controllers
{
    /// <summary>
    /// Báo cáo dành cho Quản lý (Admin):
    /// 1. Màn hình Báo cáo Doanh thu (/Report/Index)
    /// 2. Màn hình Thống kê Tỷ lệ lấp đầy (/Report/Occupancy)
    /// </summary>
    public class ReportController : Controller
    {
        // Màn hình Thống kê & Doanh thu (có chuyển trang giữa các bảng báo cáo)
        public ActionResult Index(DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            if (!AuthHelper.IsAdmin())
            {
                TempData["Error"] = "Chức năng Thống kê & Doanh thu chỉ dành cho Quản trị viên.";
                return RedirectToAction("Index", "Home");
            }

            if (page < 1 || page > 3) page = 1;
            var vm = BuildReportData(fromDate, toDate);
            ViewBag.CurrentPage = page;
            ViewBag.RoomDict = DataStore.Db.Rooms.ToDictionary(r => r.Id, r => r);
            return View(vm);
        }

        // Màn hình 2: Thống kê Tỷ lệ lấp đầy (Occupancy Rate)
        public ActionResult Occupancy(DateTime? fromDate, DateTime? toDate)
        {
            if (!AuthHelper.IsAdmin())
            {
                TempData["Error"] = "Chức năng Thống kê tỷ lệ lấp đầy chỉ dành cho Quản lý.";
                return RedirectToAction("Index", "Home");
            }

            var vm = BuildReportData(fromDate, toDate);
            return View(vm);
        }

        private BusinessReportViewModel BuildReportData(DateTime? fromDate, DateTime? toDate)
        {
            var db = DataStore.Db;
            DateTime today = DateTime.Today;

            DateTime start = fromDate.HasValue ? fromDate.Value.Date : today.AddDays(-7);
            DateTime end = toDate.HasValue ? toDate.Value.Date : today.AddDays(2);

            if (end < start)
            {
                end = start.AddDays(1);
            }

            int totalDays = (end - start).Days + 1;
            if (totalDays <= 0) totalDays = 1;

            int totalRooms = db.Rooms.Count;
            int totalCapacityNights = totalRooms * totalDays;

            var bookingsInPeriod = db.Bookings
                .Where(b => b.CheckInDate.Date <= end && b.CheckOutDate.Date >= start)
                .OrderByDescending(b => b.CheckInDate)
                .ToList();

            var validBookings = bookingsInPeriod.Where(b => b.Status != "Cancelled").ToList();

            var vm = new BusinessReportViewModel
            {
                FromDate = start,
                ToDate = end,
                TotalDays = totalDays,
                TotalRooms = totalRooms,
                TotalCapacityNights = totalCapacityNights,
                TotalBookings = bookingsInPeriod.Count,
                CompletedOrActiveBookings = validBookings.Count,
                CancelledBookings = bookingsInPeriod.Count(b => b.Status == "Cancelled"),
                TotalRoomRevenue = validBookings.Sum(b => b.RoomTotalAmount),
                TotalServiceRevenue = validBookings.Sum(b => b.ServiceTotalAmount),
                TotalExtraAndDiscountNet = validBookings.Sum(b => b.ExtraCharge - b.DiscountAmount),
                GrandTotalRevenue = validBookings.Sum(b => b.FinalTotal),
                BookingsInPeriod = bookingsInPeriod
            };

            var roomTypesDict = db.RoomTypes.ToDictionary(rt => rt.Id, rt => rt);
            int totalOccupiedNightsAllRooms = 0;

            foreach (var room in db.Rooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber))
            {
                var roomBookings = validBookings.Where(b => b.RoomId == room.Id).ToList();
                int occupiedNights = 0;

                for (int d = 0; d < totalDays; d++)
                {
                    DateTime dayDate = start.AddDays(d);
                    bool isOccupiedThatNight = roomBookings.Any(b => b.CheckInDate.Date <= dayDate && dayDate < b.CheckOutDate.Date);
                    if (isOccupiedThatNight)
                    {
                        occupiedNights++;
                    }
                }

                totalOccupiedNightsAllRooms += occupiedNights;
                double occRate = totalDays > 0 ? Math.Round((double)occupiedNights * 100.0 / totalDays, 1) : 0;

                vm.RoomOccupancies.Add(new RoomOccupancyItem
                {
                    RoomId = room.Id,
                    RoomNumber = room.RoomNumber,
                    RoomTypeName = roomTypesDict.ContainsKey(room.RoomTypeId) ? roomTypesDict[room.RoomTypeId].Name : "",
                    CurrentStatusText = room.StatusText,
                    TotalDaysInPeriod = totalDays,
                    OccupiedNights = occupiedNights,
                    OccupancyRate = occRate,
                    Revenue = roomBookings.Sum(b => b.FinalTotal)
                });
            }

            vm.TotalOccupiedNights = totalOccupiedNightsAllRooms;
            vm.OverallOccupancyRate = totalCapacityNights > 0
                ? Math.Round((double)totalOccupiedNightsAllRooms * 100.0 / totalCapacityNights, 1)
                : 0;

            foreach (var rt in db.RoomTypes)
            {
                var roomsOfType = db.Rooms.Where(r => r.RoomTypeId == rt.Id).Select(r => r.Id).ToList();
                int roomCount = roomsOfType.Count;
                int capacityNights = roomCount * totalDays;

                int occupiedNights = vm.RoomOccupancies
                    .Where(ro => roomsOfType.Contains(ro.RoomId))
                    .Sum(ro => ro.OccupiedNights);

                var bookingsOfType = validBookings.Where(b => roomsOfType.Contains(b.RoomId)).ToList();

                vm.RoomTypeReports.Add(new RoomTypeReportItem
                {
                    RoomTypeId = rt.Id,
                    RoomTypeName = rt.Name,
                    RoomCount = roomCount,
                    TotalAvailableRoomNights = capacityNights,
                    OccupiedRoomNights = occupiedNights,
                    OccupancyRate = capacityNights > 0 ? Math.Round((double)occupiedNights * 100.0 / capacityNights, 1) : 0,
                    BookingCount = bookingsOfType.Count,
                    RoomRevenue = bookingsOfType.Sum(b => b.RoomTotalAmount + b.ExtraCharge - b.DiscountAmount),
                    ServiceRevenue = bookingsOfType.Sum(b => b.ServiceTotalAmount)
                });
            }

            var validBookingIds = validBookings.Select(b => b.Id).ToList();
            var usedServices = db.BookingServices.Where(bs => validBookingIds.Contains(bs.BookingId)).ToList();

            vm.TopServices = usedServices
                .GroupBy(bs => new { bs.ServiceId, bs.ServiceName, bs.Unit })
                .Select(g =>
                {
                    var svc = db.Services.FirstOrDefault(s => s.Id == g.Key.ServiceId);
                    return new ServiceRevenueItem
                    {
                        ServiceId = g.Key.ServiceId,
                        ServiceName = g.Key.ServiceName,
                        Category = svc != null ? svc.Category : "Dịch vụ",
                        Unit = g.Key.Unit,
                        TotalQuantity = g.Sum(x => x.Quantity),
                        TotalRevenue = g.Sum(x => x.TotalAmount)
                    };
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            return vm;
        }
    }
}
