using System;
using System.Linq;
using System.Web.Mvc;
using QuanLyLuuTru.Models;

namespace QuanLyLuuTru.Controllers
{
    public class HomeController : Controller
    {
        // Trang chủ:
        // - Nếu là Nhân viên hoặc Quản lý: Chuyển thẳng về trang Danh sách phòng dạng bảng (/Room/Index)
        // - Nếu là Khách hàng: Chỉ hiển thị danh sách các phòng chưa đặt (Status == "Available")
        public ActionResult Index(int? roomTypeId)
        {
            if (AuthHelper.IsStaffOrAdmin())
            {
                return RedirectToAction("Index", "Room");
            }

            var db = DataStore.Db;
            // Lấy danh sách ID các phòng đang có đơn đặt (Booked) hoặc đang ở (CheckedIn)
            var bookedRoomIds = db.Bookings
                .Where(b => b.Status == "Booked" || b.Status == "CheckedIn")
                .Select(b => b.RoomId)
                .Distinct()
                .ToList();

            // Chỉ hiển thị các phòng chưa đặt và đang sẵn sàng cho khách hàng
            var roomsQuery = db.Rooms
                .Where(r => r.Status == "Available" && !bookedRoomIds.Contains(r.Id))
                .AsQueryable();

            if (roomTypeId.HasValue)
            {
                roomsQuery = roomsQuery.Where(r => r.RoomTypeId == roomTypeId.Value);
                ViewBag.SelectedRoomType = db.RoomTypes.FirstOrDefault(rt => rt.Id == roomTypeId.Value);
            }

            ViewBag.SelectedRoomTypeId = roomTypeId;
            ViewBag.RoomTypes = db.RoomTypes.ToDictionary(rt => rt.Id, rt => rt);

            return View(roomsQuery.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber).ToList());
        }

        // Xem chi tiết 1 phòng cụ thể
        public ActionResult RoomDetail(int id)
        {
            var db = DataStore.Db;
            var room = db.Rooms.FirstOrDefault(r => r.Id == id);
            if (room == null) return HttpNotFound();

            var roomType = db.RoomTypes.FirstOrDefault(rt => rt.Id == room.RoomTypeId);

            // Các phòng trống khác cùng loại
            var relatedRooms = db.Rooms
                .Where(r => r.RoomTypeId == room.RoomTypeId && r.Id != room.Id && r.Status == "Available")
                .ToList();

            ViewBag.RoomType = roomType;
            ViewBag.RelatedRooms = relatedRooms;
            ViewBag.ServiceList = db.Services.Where(s => s.IsActive).ToList();

            return View(room);
        }

        [HttpPost]
        public ActionResult ResetDemoData()
        {
            DataStore.ResetToSeedData();
            TempData["Success"] = "Đã khôi phục dữ liệu mẫu ban đầu thành công!";
            return RedirectToAction("Index");
        }
    }
}
