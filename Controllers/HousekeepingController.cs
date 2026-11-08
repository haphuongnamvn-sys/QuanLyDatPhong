using System;
using System.Linq;
using System.Web.Mvc;
using QuanLyLuuTru.Models;

namespace QuanLyLuuTru.Controllers
{
    /// <summary>
    /// Quản lý công việc dọn phòng (Tách riêng màn hình Danh sách và màn hình Tạo phiếu dọn phòng)
    /// </summary>
    public class HousekeepingController : Controller
    {
        public ActionResult Index(string status)
        {
            if (!AuthHelper.IsStaffOrAdmin())
            {
                TempData["Error"] = "Chức năng Dọn phòng chỉ dành cho Quản lý và Nhân viên.";
                return RedirectToAction("Login", "Account");
            }

            var db = DataStore.Db;
            var query = db.HousekeepingTasks.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            ViewBag.SelectedStatus = status;
            ViewBag.RoomDict = db.Rooms.ToDictionary(r => r.Id, r => r);

            var list = query
                .OrderBy(t => t.Status == "Completed" ? 1 : 0)
                .ThenByDescending(t => t.CreatedAt)
                .ToList();

            return View(list);
        }

        // Màn hình riêng: Tạo phiếu dọn phòng mới
        public ActionResult Create()
        {
            if (!AuthHelper.IsStaffOrAdmin()) return RedirectToAction("Login", "Account");

            ViewBag.Rooms = DataStore.Db.Rooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber).ToList();
            return View(new HousekeepingTask { Priority = "Bình thường", TaskType = "Dọn sau trả phòng" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int roomId, string staffName, string taskType, string priority, string notes)
        {
            if (!AuthHelper.IsStaffOrAdmin()) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var room = db.Rooms.FirstOrDefault(r => r.Id == roomId);
            if (room != null)
            {
                int nextId = db.HousekeepingTasks.Any() ? db.HousekeepingTasks.Max(t => t.Id) + 1 : 1;
                db.HousekeepingTasks.Add(new HousekeepingTask
                {
                    Id = nextId,
                    RoomId = roomId,
                    StaffName = string.IsNullOrWhiteSpace(staffName) ? "Cô Nguyễn Thị Lan" : staffName.Trim(),
                    TaskType = string.IsNullOrWhiteSpace(taskType) ? "Dọn vệ sinh phòng" : taskType,
                    Priority = string.IsNullOrWhiteSpace(priority) ? "Bình thường" : priority,
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    Notes = notes
                });

                if (room.Status == "Available")
                {
                    room.Status = "Cleaning";
                }

                DataStore.SaveChanges();
                TempData["Success"] = "Đã tạo phiếu dọn phòng cho phòng " + room.RoomNumber + "!";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string newStatus, string staffName, string notes)
        {
            if (!AuthHelper.IsStaffOrAdmin()) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var task = db.HousekeepingTasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return HttpNotFound();

            task.Status = newStatus;
            if (!string.IsNullOrWhiteSpace(staffName)) task.StaffName = staffName.Trim();
            if (!string.IsNullOrWhiteSpace(notes)) task.Notes = notes.Trim();

            var room = db.Rooms.FirstOrDefault(r => r.Id == task.RoomId);

            if (newStatus == "Completed")
            {
                task.CompletedAt = DateTime.Now;
                if (room != null && room.Status == "Cleaning")
                {
                    room.Status = "Available";
                    room.Notes = "Đã dọn sạch lúc " + DateTime.Now.ToString("HH:mm dd/MM");
                }
                TempData["Success"] = "Đã dọn xong phòng " + (room != null ? room.RoomNumber : "") + "! Phòng đã chuyển về trạng thái Phòng trống.";
            }
            else if (newStatus == "InProgress")
            {
                TempData["Success"] = "Đang tiến hành dọn phòng " + (room != null ? room.RoomNumber : "") + ".";
            }

            DataStore.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (!AuthHelper.IsStaffOrAdmin()) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var task = db.HousekeepingTasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                db.HousekeepingTasks.Remove(task);
                DataStore.SaveChanges();
                TempData["Success"] = "Đã xóa công việc dọn phòng.";
            }
            return RedirectToAction("Index");
        }
    }
}
