using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using QuanLyLuuTru.Models;

namespace QuanLyLuuTru.Controllers
{
    /// <summary>
    /// - Quản trị viên (Admin): Quản lý Loại phòng (Thêm/Sửa/Xóa) & Quản lý Phòng (Thêm/Sửa/Xóa)
    /// - Nhân viên (Staff): Xem Danh sách phòng & Kiểm tra tình trạng phòng theo khoảng thời gian
    /// </summary>
    public class RoomController : Controller
    {
        // Danh sách phòng
        public ActionResult Index(DateTime? checkIn, DateTime? checkOut, int? roomTypeId)
        {
            if (!AuthHelper.IsStaffOrAdmin())
            {
                TempData["Error"] = "Vui lòng đăng nhập tài khoản Quản trị viên hoặc Nhân viên.";
                return RedirectToAction("Login", "Account");
            }

            var db = DataStore.Db;
            DateTime start = checkIn.HasValue ? checkIn.Value.Date : DateTime.Today;
            DateTime end = checkOut.HasValue && checkOut.Value.Date > start ? checkOut.Value.Date : start.AddDays(1);
            int nights = DataStore.CalculateNights(start, end);

            var rooms = db.Rooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber).ToList();
            if (roomTypeId.HasValue)
            {
                rooms = rooms.Where(r => r.RoomTypeId == roomTypeId.Value).ToList();
            }

            var results = new List<RoomAvailabilityResult>();
            foreach (var room in rooms)
            {
                var roomType = db.RoomTypes.FirstOrDefault(rt => rt.Id == room.RoomTypeId);
                var conflict = DataStore.GetConflictingBooking(room.Id, start, end);
                bool available = true;
                string reason = "Phòng còn trống";

                if (room.Status == "Maintenance")
                {
                    available = false;
                    reason = "Phòng đang bảo trì";
                }
                else if (room.Status == "Cleaning")
                {
                    available = false;
                    reason = "Phòng đang chờ dọn vệ sinh";
                }
                else if (conflict != null)
                {
                    available = false;
                    reason = string.Format("Trùng đơn {0} ({1}: {2:dd/MM} - {3:dd/MM})",
                        conflict.BookingCode, conflict.CustomerName, conflict.CheckInDate, conflict.CheckOutDate);
                }

                decimal price = room.PricePerNight > 0 ? room.PricePerNight : (roomType != null ? roomType.BasePrice : 300000);

                results.Add(new RoomAvailabilityResult
                {
                    Room = room,
                    RoomType = roomType,
                    IsAvailable = available,
                    ConflictReason = reason,
                    ConflictingBooking = conflict,
                    Nights = nights,
                    EstimatedRoomCharge = nights * price
                });
            }

            ViewBag.CheckIn = start.ToString("yyyy-MM-dd");
            ViewBag.CheckOut = end.ToString("yyyy-MM-dd");
            ViewBag.Nights = nights;
            ViewBag.SelectedRoomTypeId = roomTypeId;
            ViewBag.RoomTypes = db.RoomTypes.OrderBy(rt => rt.BasePrice).ToList();
            ViewBag.IsAdmin = AuthHelper.IsAdmin();

            return View(results);
        }

        public ActionResult StatusMap()
        {
            return RedirectToAction("Index");
        }

        // Thêm, sửa, xóa phòng (Quản trị viên)
        public ActionResult CreateRoom()
        {
            if (!AuthHelper.IsAdmin())
            {
                TempData["Error"] = "Chỉ Quản trị viên mới có quyền thêm phòng mới.";
                return RedirectToAction("Index");
            }
            ViewBag.RoomTypes = new SelectList(DataStore.Db.RoomTypes, "Id", "Name");
            return View(new Room
            {
                Floor = 1,
                RoomTypeId = 1,
                CategoryName = "Phòng Đơn Tiêu Chuẩn",
                PricePerNight = 300000,
                MaxGuests = 2,
                Description = "Giường đôi 1m6, điều hòa, tivi, bình nóng lạnh, wifi",
                Status = "Available"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRoom(Room model)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("Index");

            var db = DataStore.Db;
            if (string.IsNullOrWhiteSpace(model.RoomNumber))
            {
                ModelState.AddModelError("RoomNumber", "Vui lòng nhập số phòng.");
            }
            else if (db.Rooms.Any(r => r.RoomNumber.Equals(model.RoomNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("RoomNumber", "Số phòng này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                var rt = db.RoomTypes.FirstOrDefault(x => x.Id == model.RoomTypeId);
                model.Id = db.Rooms.Any() ? db.Rooms.Max(r => r.Id) + 1 : 1;
                model.RoomNumber = model.RoomNumber.Trim().ToUpper();
                model.CategoryName = rt != null ? rt.Name : "Phòng tiêu chuẩn";
                if (model.PricePerNight <= 0) model.PricePerNight = rt != null ? rt.BasePrice : 300000;
                if (model.MaxGuests <= 0) model.MaxGuests = rt != null ? rt.MaxGuests : 2;
                if (string.IsNullOrWhiteSpace(model.Description) && rt != null) model.Description = rt.Description;
                if (string.IsNullOrEmpty(model.Status)) model.Status = "Available";

                db.Rooms.Add(model);
                DataStore.SaveChanges();
                TempData["Success"] = "Đã thêm phòng " + model.RoomNumber + " thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.RoomTypes = new SelectList(db.RoomTypes, "Id", "Name", model.RoomTypeId);
            return View(model);
        }

        public ActionResult EditRoom(int id)
        {
            if (!AuthHelper.IsAdmin())
            {
                TempData["Error"] = "Chỉ Quản trị viên mới có quyền sửa thông tin phòng.";
                return RedirectToAction("Index");
            }

            var db = DataStore.Db;
            var room = db.Rooms.FirstOrDefault(r => r.Id == id);
            if (room == null) return HttpNotFound();

            var rt = db.RoomTypes.FirstOrDefault(x => x.Id == room.RoomTypeId);
            if (room.PricePerNight <= 0 && rt != null) room.PricePerNight = rt.BasePrice;
            if (room.MaxGuests <= 0 && rt != null) room.MaxGuests = rt.MaxGuests;
            if (string.IsNullOrEmpty(room.Description) && rt != null) room.Description = rt.Description;

            ViewBag.RoomTypes = new SelectList(db.RoomTypes, "Id", "Name", room.RoomTypeId);
            ViewBag.IsAdmin = true;
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRoom(Room model)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("Index");

            var db = DataStore.Db;
            var room = db.Rooms.FirstOrDefault(r => r.Id == model.Id);
            if (room == null) return HttpNotFound();

            if (db.Rooms.Any(r => r.Id != model.Id && r.RoomNumber.Equals(model.RoomNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("RoomNumber", "Số phòng này đã trùng với phòng khác.");
            }

            if (ModelState.IsValid)
            {
                var rt = db.RoomTypes.FirstOrDefault(x => x.Id == model.RoomTypeId);
                room.RoomNumber = model.RoomNumber.Trim().ToUpper();
                room.Floor = model.Floor;
                room.RoomTypeId = model.RoomTypeId;
                room.CategoryName = rt != null ? rt.Name : room.CategoryName;
                room.PricePerNight = model.PricePerNight > 0 ? model.PricePerNight : room.PricePerNight;
                room.MaxGuests = model.MaxGuests > 0 ? model.MaxGuests : room.MaxGuests;
                room.Description = model.Description;
                room.Status = model.Status;
                room.Notes = model.Notes;

                DataStore.SaveChanges();
                TempData["Success"] = "Đã cập nhật thông tin phòng " + room.RoomNumber + "!";
                return RedirectToAction("Index");
            }

            ViewBag.RoomTypes = new SelectList(db.RoomTypes, "Id", "Name", model.RoomTypeId);
            ViewBag.IsAdmin = true;
            return View(model);
        }

        [HttpPost]
        public ActionResult DeleteRoom(int id)
        {
            if (!AuthHelper.IsAdmin())
            {
                TempData["Error"] = "Chỉ Quản trị viên mới có quyền xóa phòng.";
                return RedirectToAction("Index");
            }

            var db = DataStore.Db;
            var room = db.Rooms.FirstOrDefault(r => r.Id == id);
            if (room != null)
            {
                bool hasActiveBooking = db.Bookings.Any(b => b.RoomId == id && (b.Status == "Booked" || b.Status == "CheckedIn"));
                if (hasActiveBooking)
                {
                    TempData["Error"] = "Không thể xóa phòng " + room.RoomNumber + " vì đang có lịch đặt hoặc khách đang ở!";
                }
                else
                {
                    db.Rooms.Remove(room);
                    DataStore.SaveChanges();
                    TempData["Success"] = "Đã xóa phòng " + room.RoomNumber + ".";
                }
            }
            return RedirectToAction("Index");
        }

        // Quản lý loại phòng (Quản trị viên)
        public ActionResult RoomTypes()
        {
            if (!AuthHelper.IsAdmin())
            {
                TempData["Error"] = "Chức năng Quản lý loại phòng chỉ dành cho Quản trị viên.";
                return RedirectToAction("Index", "Home");
            }

            var db = DataStore.Db;
            ViewBag.IsAdmin = true;
            ViewBag.RoomCounts = db.Rooms
                .GroupBy(r => r.RoomTypeId)
                .ToDictionary(g => g.Key, g => g.Count());

            return View(db.RoomTypes.OrderBy(rt => rt.BasePrice).ToList());
        }

        public ActionResult CreateRoomType()
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("RoomTypes");
            return View(new RoomType { MaxGuests = 2, BasePrice = 300000, WeekendPrice = 350000, LateCheckoutFeePerHour = 50000 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRoomType(RoomType model)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("RoomTypes");

            var db = DataStore.Db;
            if (ModelState.IsValid)
            {
                model.Id = db.RoomTypes.Any() ? db.RoomTypes.Max(rt => rt.Id) + 1 : 1;
                db.RoomTypes.Add(model);
                DataStore.SaveChanges();
                TempData["Success"] = "Đã thêm loại phòng: " + model.Name;
                return RedirectToAction("RoomTypes");
            }
            return View(model);
        }

        public ActionResult EditRoomType(int id)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("RoomTypes");
            var rt = DataStore.Db.RoomTypes.FirstOrDefault(x => x.Id == id);
            if (rt == null) return HttpNotFound();
            return View(rt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRoomType(RoomType model)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("RoomTypes");

            var db = DataStore.Db;
            var rt = db.RoomTypes.FirstOrDefault(x => x.Id == model.Id);
            if (rt == null) return HttpNotFound();

            if (ModelState.IsValid)
            {
                rt.Name = model.Name;
                rt.BasePrice = model.BasePrice;
                rt.WeekendPrice = model.WeekendPrice;
                rt.LateCheckoutFeePerHour = model.LateCheckoutFeePerHour;
                rt.MaxGuests = model.MaxGuests;
                rt.Description = model.Description;

                // Đồng bộ lại thông tin trên các phòng thuộc loại phòng này
                foreach (var r in db.Rooms.Where(x => x.RoomTypeId == rt.Id))
                {
                    r.CategoryName = rt.Name;
                    r.PricePerNight = rt.BasePrice;
                    r.MaxGuests = rt.MaxGuests;
                    r.Description = rt.Description;
                }

                DataStore.SaveChanges();
                TempData["Success"] = "Đã cập nhật loại phòng: " + rt.Name;
                return RedirectToAction("RoomTypes");
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult DeleteRoomType(int id)
        {
            if (!AuthHelper.IsAdmin()) return RedirectToAction("RoomTypes");

            var db = DataStore.Db;
            var rt = db.RoomTypes.FirstOrDefault(x => x.Id == id);
            if (rt != null)
            {
                if (db.Rooms.Any(r => r.RoomTypeId == id))
                {
                    TempData["Error"] = "Không thể xóa loại phòng '" + rt.Name + "' vì đang có phòng thuộc loại này!";
                }
                else
                {
                    db.RoomTypes.Remove(rt);
                    DataStore.SaveChanges();
                    TempData["Success"] = "Đã xóa loại phòng '" + rt.Name + "'.";
                }
            }
            return RedirectToAction("RoomTypes");
        }
    }
}
