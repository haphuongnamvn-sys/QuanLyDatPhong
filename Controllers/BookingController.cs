using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using QuanLyLuuTru.Models;

namespace QuanLyLuuTru.Controllers
{
    /// <summary>
    /// Quản lý Đặt phòng & Đặt cọc, Tìm phòng trống, Thay đổi/Hủy đơn, Nhận phòng, Dịch vụ phát sinh và Trả phòng
    /// (Mỗi chức năng được tách thành màn hình riêng biệt)
    /// </summary>
    public class BookingController : Controller
    {
        // Danh sách đơn đặt phòng
        public ActionResult Index(string status, string keyword)
        {
            if (AuthHelper.IsCustomer())
            {
                return RedirectToAction("MyBookings");
            }
            if (!AuthHelper.IsStaffOrAdmin())
            {
                TempData["Error"] = "Vui lòng đăng nhập tài khoản Nhân viên hoặc Quản lý.";
                return RedirectToAction("Login", "Account");
            }

            var db = DataStore.Db;
            var query = db.Bookings.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(b =>
                    (b.BookingCode != null && b.BookingCode.ToLower().Contains(kw)) ||
                    (b.CustomerName != null && b.CustomerName.ToLower().Contains(kw)) ||
                    (b.CustomerPhone != null && b.CustomerPhone.ToLower().Contains(kw)));
            }

            ViewBag.SelectedStatus = status;
            ViewBag.Keyword = keyword;
            ViewBag.RoomDict = db.Rooms.ToDictionary(r => r.Id, r => r);
            ViewBag.RoomTypeDict = db.RoomTypes.ToDictionary(rt => rt.Id, rt => rt);

            return View(query.OrderByDescending(b => b.Id).ToList());
        }

        // Đơn đặt phòng của khách hàng
        public ActionResult MyBookings()
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem đơn đặt phòng của bạn.";
                return RedirectToAction("Login", "Account");
            }

            var db = DataStore.Db;
            var myBookings = db.Bookings
                .Where(b => b.CustomerUserId == currentUser.Id ||
                           (!string.IsNullOrEmpty(currentUser.Phone) && b.CustomerPhone == currentUser.Phone))
                .OrderByDescending(b => b.Id)
                .ToList();

            ViewBag.RoomDict = db.Rooms.ToDictionary(r => r.Id, r => r);
            ViewBag.RoomTypeDict = db.RoomTypes.ToDictionary(rt => rt.Id, rt => rt);

            return View(myBookings);
        }

        // Kiểm tra phòng trống theo khoảng thời gian
        public ActionResult CheckAvailability(DateTime? checkIn, DateTime? checkOut, int? roomTypeId)
        {
            var db = DataStore.Db;
            DateTime start = checkIn.HasValue ? checkIn.Value.Date : DateTime.Today;
            DateTime end = checkOut.HasValue ? checkOut.Value.Date : start.AddDays(1);

            if (end <= start)
            {
                end = start.AddDays(1);
                TempData["Error"] = "Ngày trả phòng phải sau ngày nhận phòng ít nhất 1 đêm.";
            }

            int nights = DataStore.CalculateNights(start, end);
            bool isStaffOrAdmin = AuthHelper.IsStaffOrAdmin();
            var results = new List<RoomAvailabilityResult>();

            var rooms = db.Rooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber).ToList();
            if (roomTypeId.HasValue)
            {
                rooms = rooms.Where(r => r.RoomTypeId == roomTypeId.Value).ToList();
            }

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
                else if (conflict != null)
                {
                    available = false;
                    reason = isStaffOrAdmin
                        ? string.Format("Trùng đơn {0} ({1}: {2:dd/MM} - {3:dd/MM})", conflict.BookingCode, conflict.CustomerName, conflict.CheckInDate, conflict.CheckOutDate)
                        : string.Format("Đã có lịch đặt từ {0:dd/MM/yyyy} đến {1:dd/MM/yyyy}", conflict.CheckInDate, conflict.CheckOutDate);
                }

                results.Add(new RoomAvailabilityResult
                {
                    Room = room,
                    RoomType = roomType,
                    IsAvailable = available,
                    ConflictReason = reason,
                    ConflictingBooking = isStaffOrAdmin ? conflict : null,
                    Nights = nights,
                    EstimatedRoomCharge = DataStore.CalculateRoomCharge(roomType, start, end)
                });
            }

            ViewBag.CheckIn = start.ToString("yyyy-MM-dd");
            ViewBag.CheckOut = end.ToString("yyyy-MM-dd");
            ViewBag.Nights = nights;
            ViewBag.SelectedRoomTypeId = roomTypeId;
            ViewBag.RoomTypes = db.RoomTypes;
            ViewBag.IsStaffOrAdmin = isStaffOrAdmin;

            return View(results);
        }

        // Đặt phòng và đặt cọc
        public ActionResult Create(int? roomId, DateTime? checkIn, DateTime? checkOut)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập hoặc đăng ký tài khoản để tiến hành đặt phòng.";
                return RedirectToAction("Login", "Account");
            }

            var db = DataStore.Db;
            DateTime start = checkIn.HasValue ? checkIn.Value.Date : DateTime.Today;
            DateTime end = checkOut.HasValue && checkOut.Value.Date > start ? checkOut.Value.Date : start.AddDays(1);

            int defaultRoomId = roomId ?? (db.Rooms.FirstOrDefault(r => r.Status == "Available")?.Id ?? 1);
            var selectedRoom = db.Rooms.FirstOrDefault(r => r.Id == defaultRoomId);
            var selectedRoomType = selectedRoom != null ? db.RoomTypes.FirstOrDefault(rt => rt.Id == selectedRoom.RoomTypeId) : null;

            decimal pricePerNight = selectedRoom != null && selectedRoom.PricePerNight > 0
                ? selectedRoom.PricePerNight
                : (selectedRoomType != null ? selectedRoomType.BasePrice : 300000);
            int nights = DataStore.CalculateNights(start, end);
            decimal estimatedTotal = nights * pricePerNight;
            decimal suggestedDeposit = Math.Round(estimatedTotal * 0.3m, 0);

            var model = new Booking
            {
                RoomId = defaultRoomId,
                CheckInDate = start,
                CheckOutDate = end,
                GuestCount = 2,
                RoomPricePerNight = pricePerNight,
                TotalNights = nights,
                RoomTotalAmount = estimatedTotal,
                DepositAmount = suggestedDeposit,
                Status = "Booked"
            };

            if (currentUser.Role == "Customer")
            {
                model.CustomerName = currentUser.FullName;
                model.CustomerPhone = currentUser.Phone;
                model.CustomerIdentityCard = currentUser.IdentityCard;
            }

            ViewBag.SelectedRoom = selectedRoom;
            ViewBag.SelectedRoomType = selectedRoomType;
            ViewBag.IsStaffOrAdmin = AuthHelper.IsStaffOrAdmin();
            PrepareRoomDropdown(defaultRoomId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Booking model, string depositMethod = "Chuyển khoản ngân hàng", bool checkInNow = false)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;

            if (model.CheckOutDate.Date <= model.CheckInDate.Date)
            {
                ModelState.AddModelError("CheckOutDate", "Ngày trả phòng phải sau ngày nhận phòng ít nhất 1 ngày.");
            }

            var room = db.Rooms.FirstOrDefault(r => r.Id == model.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "Phòng chọn không hợp lệ.");
            }
            else if (room.Status == "Maintenance")
            {
                ModelState.AddModelError("RoomId", "Phòng " + room.RoomNumber + " đang bảo trì, không thể đặt.");
            }
            else
            {
                var conflict = DataStore.GetConflictingBooking(model.RoomId, model.CheckInDate, model.CheckOutDate);
                if (conflict != null)
                {
                    ModelState.AddModelError("", string.Format(
                        "Phòng {0} đã có người đặt từ ngày {1:dd/MM/yyyy} đến {2:dd/MM/yyyy}. Vui lòng chọn phòng khác hoặc đổi ngày!",
                        room.RoomNumber, conflict.CheckInDate, conflict.CheckOutDate));
                }
            }

            if (ModelState.IsValid)
            {
                var roomType = db.RoomTypes.FirstOrDefault(rt => rt.Id == room.RoomTypeId);
                decimal pricePerNight = room.PricePerNight > 0 ? room.PricePerNight : (roomType != null ? roomType.BasePrice : 300000);
                int nights = DataStore.CalculateNights(model.CheckInDate, model.CheckOutDate);
                decimal roomTotal = nights * pricePerNight;

                int nextId = db.Bookings.Any() ? db.Bookings.Max(b => b.Id) + 1 : 1;
                model.Id = nextId;
                model.BookingCode = string.Format("DP{0:D3}", nextId);
                model.CheckInDate = model.CheckInDate.Date;
                model.CheckOutDate = model.CheckOutDate.Date;
                model.TotalNights = nights;
                model.RoomPricePerNight = pricePerNight;
                model.RoomTotalAmount = roomTotal;
                model.ServiceTotalAmount = 0;
                model.CreatedAt = DateTime.Now;

                if (model.DepositAmount > 0 && !string.IsNullOrEmpty(depositMethod))
                {
                    model.Notes = string.IsNullOrWhiteSpace(model.Notes)
                        ? "Đặt cọc qua: " + depositMethod
                        : model.Notes + " (Đặt cọc qua: " + depositMethod + ")";
                }

                if (currentUser.Role == "Customer")
                {
                    model.CustomerUserId = currentUser.Id;
                    model.Status = "Booked";
                }
                else if (checkInNow && AuthHelper.IsStaffOrAdmin())
                {
                    model.Status = "CheckedIn";
                    model.ActualCheckIn = DateTime.Now;
                    room.Status = "Occupied";
                }
                else
                {
                    model.Status = "Booked";
                }

                db.Bookings.Add(model);
                DataStore.SaveChanges();

                TempData["Success"] = string.Format("Đặt phòng thành công! Mã đơn: {0} - Phòng {1} (Đã ghi nhận tiền cọc: {2:N0}đ).",
                    model.BookingCode, room.RoomNumber, model.DepositAmount);
                return RedirectToAction("Details", new { id = model.Id });
            }

            ViewBag.SelectedRoom = room;
            ViewBag.SelectedRoomType = room != null ? db.RoomTypes.FirstOrDefault(rt => rt.Id == room.RoomTypeId) : null;
            ViewBag.IsStaffOrAdmin = AuthHelper.IsStaffOrAdmin();
            PrepareRoomDropdown(model.RoomId);
            return View(model);
        }

        // Chi tiết đơn đặt phòng
        public ActionResult Details(int id)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var booking = db.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking == null) return HttpNotFound();

            if (!CanAccessBooking(booking, currentUser))
            {
                TempData["Error"] = "Bạn không có quyền xem đơn đặt phòng này.";
                return RedirectToAction("MyBookings");
            }

            var room = db.Rooms.FirstOrDefault(r => r.Id == booking.RoomId);
            var roomType = room != null ? db.RoomTypes.FirstOrDefault(rt => rt.Id == room.RoomTypeId) : null;
            var usedServices = db.BookingServices.Where(bs => bs.BookingId == id).OrderByDescending(bs => bs.CreatedAt).ToList();

            ViewBag.Room = room;
            ViewBag.RoomType = roomType;
            ViewBag.UsedServices = usedServices;
            ViewBag.IsStaffOrAdmin = AuthHelper.IsStaffOrAdmin();

            return View(booking);
        }

        // Thay đổi ngày lưu trú hoặc đổi phòng
        public ActionResult Edit(int id)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var booking = DataStore.Db.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking == null) return HttpNotFound();

            if (!CanAccessBooking(booking, currentUser)) return RedirectToAction("Index", "Home");

            if (currentUser.Role == "Customer" && booking.Status != "Booked")
            {
                TempData["Error"] = "Khách hàng chỉ được đổi lịch khi đơn đang ở trạng thái Chờ nhận phòng.";
                return RedirectToAction("Details", new { id = booking.Id });
            }

            if (booking.Status == "Cancelled" || booking.Status == "CheckedOut")
            {
                TempData["Error"] = "Đơn đã kết thúc hoặc đã hủy, không thể sửa.";
                return RedirectToAction("Details", new { id = booking.Id });
            }

            PrepareRoomDropdown(booking.RoomId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Booking model)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var existing = db.Bookings.FirstOrDefault(b => b.Id == model.Id);
            if (existing == null) return HttpNotFound();

            if (!CanAccessBooking(existing, currentUser)) return RedirectToAction("Index", "Home");

            if (model.CheckOutDate.Date <= model.CheckInDate.Date)
            {
                ModelState.AddModelError("CheckOutDate", "Ngày trả phòng phải sau ngày nhận phòng ít nhất 1 đêm.");
            }

            var newRoom = db.Rooms.FirstOrDefault(r => r.Id == model.RoomId);
            if (newRoom == null || newRoom.Status == "Maintenance")
            {
                ModelState.AddModelError("RoomId", "Phòng được chọn đang bảo trì.");
            }
            else
            {
                var conflict = DataStore.GetConflictingBooking(model.RoomId, model.CheckInDate, model.CheckOutDate, existing.Id);
                if (conflict != null)
                {
                    ModelState.AddModelError("", string.Format(
                        "Không thể đổi: Phòng {0} đã có lịch đặt từ {1:dd/MM/yyyy} đến {2:dd/MM/yyyy}.",
                        newRoom.RoomNumber, conflict.CheckInDate, conflict.CheckOutDate));
                }
            }

            if (ModelState.IsValid)
            {
                if (existing.Status == "CheckedIn" && existing.RoomId != model.RoomId)
                {
                    var oldRoom = db.Rooms.FirstOrDefault(r => r.Id == existing.RoomId);
                    if (oldRoom != null) oldRoom.Status = "Cleaning";
                    newRoom.Status = "Occupied";
                }

                var roomType = db.RoomTypes.FirstOrDefault(rt => rt.Id == newRoom.RoomTypeId);
                existing.CustomerName = model.CustomerName;
                existing.CustomerPhone = model.CustomerPhone;
                existing.CustomerIdentityCard = model.CustomerIdentityCard;
                existing.GuestCount = model.GuestCount;
                existing.RoomId = model.RoomId;
                existing.CheckInDate = model.CheckInDate.Date;
                existing.CheckOutDate = model.CheckOutDate.Date;
                existing.TotalNights = DataStore.CalculateNights(existing.CheckInDate, existing.CheckOutDate);
                existing.RoomPricePerNight = roomType != null ? roomType.BasePrice : existing.RoomPricePerNight;
                existing.RoomTotalAmount = DataStore.CalculateRoomCharge(roomType, existing.CheckInDate, existing.CheckOutDate);
                if (AuthHelper.IsStaffOrAdmin())
                {
                    existing.DepositAmount = model.DepositAmount;
                }
                existing.Notes = model.Notes;

                DataStore.SaveChanges();
                TempData["Success"] = "Đã cập nhật thay đổi cho đơn " + existing.BookingCode + "!";
                return RedirectToAction("Details", new { id = existing.Id });
            }

            PrepareRoomDropdown(model.RoomId);
            return View(model);
        }

        // Hủy đặt phòng
        public ActionResult Cancel(int id)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var booking = DataStore.Db.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking == null) return HttpNotFound();

            if (!CanAccessBooking(booking, currentUser)) return RedirectToAction("Index", "Home");

            ViewBag.Room = DataStore.Db.Rooms.FirstOrDefault(r => r.Id == booking.RoomId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(int id, string cancelReason)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var booking = db.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking == null) return HttpNotFound();

            if (!CanAccessBooking(booking, currentUser)) return RedirectToAction("Index", "Home");

            if (currentUser.Role == "Customer" && booking.Status != "Booked")
            {
                TempData["Error"] = "Chỉ có thể hủy đơn khi chưa nhận phòng.";
                return RedirectToAction("Details", new { id });
            }

            if (booking.Status == "CheckedIn")
            {
                var room = db.Rooms.FirstOrDefault(r => r.Id == booking.RoomId);
                if (room != null) room.Status = "Available";
            }

            booking.Status = "Cancelled";
            string reasonText = string.IsNullOrWhiteSpace(cancelReason) ? "Khách yêu cầu hủy" : cancelReason.Trim();
            booking.Notes = string.Format("[Đã hủy {0:dd/MM HH:mm} - Lý do: {1}] {2}", DateTime.Now, reasonText, booking.Notes);

            DataStore.SaveChanges();
            TempData["Success"] = "Đã hủy đơn đặt phòng " + booking.BookingCode + " và giải phóng lịch phòng.";
            return RedirectToAction("Details", new { id });
        }

        // Xác nhận khách nhận phòng (Check-in)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckIn(int id)
        {
            if (!AuthHelper.IsStaffOrAdmin()) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var booking = db.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking == null) return HttpNotFound();

            var room = db.Rooms.FirstOrDefault(r => r.Id == booking.RoomId);
            if (room != null && room.Status == "Cleaning")
            {
                TempData["Error"] = "Phòng " + room.RoomNumber + " đang chờ dọn vệ sinh! Vui lòng hoàn tất dọn phòng trước khi cho khách nhận phòng.";
                return RedirectToAction("Details", new { id });
            }

            booking.Status = "CheckedIn";
            booking.ActualCheckIn = DateTime.Now;
            if (room != null)
            {
                room.Status = "Occupied";
            }

            DataStore.SaveChanges();
            TempData["Success"] = "Đã xác nhận khách '" + booking.CustomerName + "' nhận phòng " + (room != null ? room.RoomNumber : "") + "!";
            return RedirectToAction("Details", new { id });
        }

        // Đặt thêm dịch vụ phát sinh
        public ActionResult AddService(int id)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var booking = db.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking == null) return HttpNotFound();

            if (!CanAccessBooking(booking, currentUser)) return RedirectToAction("Index", "Home");

            ViewBag.Room = db.Rooms.FirstOrDefault(r => r.Id == booking.RoomId);
            ViewBag.UsedServices = db.BookingServices.Where(bs => bs.BookingId == id).OrderByDescending(bs => bs.CreatedAt).ToList();
            ViewBag.AvailableServices = db.Services.Where(s => s.IsActive).OrderBy(s => s.Category).ThenBy(s => s.Name).ToList();
            ViewBag.IsStaffOrAdmin = AuthHelper.IsStaffOrAdmin();

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddService(int bookingId, int serviceId, int quantity, string note)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var booking = db.Bookings.FirstOrDefault(b => b.Id == bookingId);
            var service = db.Services.FirstOrDefault(s => s.Id == serviceId);

            if (booking == null || !CanAccessBooking(booking, currentUser))
            {
                return RedirectToAction("Index", "Home");
            }

            if (service != null && quantity > 0)
            {
                bool isStaff = AuthHelper.IsStaffOrAdmin();
                int nextId = db.BookingServices.Any() ? db.BookingServices.Max(x => x.Id) + 1 : 1;
                db.BookingServices.Add(new BookingService
                {
                    Id = nextId,
                    BookingId = bookingId,
                    ServiceId = service.Id,
                    ServiceName = service.Name,
                    Unit = service.Unit,
                    Quantity = quantity,
                    UnitPrice = service.Price,
                    Note = note,
                    CreatedAt = DateTime.Now,
                    // Khách hàng đặt thì ở trạng thái Pending (Chờ nhân viên xác nhận)
                    Status = isStaff ? "Confirmed" : "Pending"
                });

                DataStore.RecalculateBookingServiceTotal(bookingId);
                if (isStaff)
                {
                    TempData["Success"] = string.Format("Đã thêm và xác nhận dịch vụ '{0}' (SL: {1}) cho đơn {2}!", service.Name, quantity, booking.BookingCode);
                }
                else
                {
                    TempData["Success"] = string.Format("Đã gửi yêu cầu đặt dịch vụ '{0}' (SL: {1}). Vui lòng chờ Nhân viên xác nhận!", service.Name, quantity);
                }
            }
            return RedirectToAction("AddService", new { id = bookingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveService(int id, int bookingId)
        {
            var currentUser = AuthHelper.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var booking = db.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null || !CanAccessBooking(booking, currentUser)) return RedirectToAction("Index", "Home");

            var item = db.BookingServices.FirstOrDefault(x => x.Id == id && x.BookingId == bookingId);
            if (item != null)
            {
                if (currentUser.Role == "Customer" && item.Status == "Confirmed")
                {
                    TempData["Error"] = "Dịch vụ đã được nhân viên xác nhận, không thể tự xóa.";
                    return RedirectToAction("AddService", new { id = bookingId });
                }

                db.BookingServices.Remove(item);
                DataStore.RecalculateBookingServiceTotal(bookingId);
                TempData["Success"] = "Đã hủy yêu cầu dịch vụ '" + item.ServiceName + "'.";
            }
            return RedirectToAction("AddService", new { id = bookingId });
        }

        // Trả phòng và thanh toán hóa đơn
        public ActionResult CheckOut(int id)
        {
            if (!AuthHelper.IsStaffOrAdmin()) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var booking = db.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking == null) return HttpNotFound();

            var room = db.Rooms.FirstOrDefault(r => r.Id == booking.RoomId);
            var roomType = room != null ? db.RoomTypes.FirstOrDefault(rt => rt.Id == room.RoomTypeId) : null;
            var usedServices = db.BookingServices.Where(bs => bs.BookingId == id).ToList();

            ViewBag.Room = room;
            ViewBag.RoomType = roomType;
            ViewBag.UsedServices = usedServices;

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckOut(int id, decimal extraCharge = 0, decimal discountAmount = 0, string checkoutNote = "")
        {
            if (!AuthHelper.IsStaffOrAdmin()) return RedirectToAction("Login", "Account");

            var db = DataStore.Db;
            var booking = db.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking == null) return HttpNotFound();

            booking.ExtraCharge = extraCharge >= 0 ? extraCharge : 0;
            booking.DiscountAmount = discountAmount >= 0 ? discountAmount : 0;
            booking.ActualCheckOut = DateTime.Now;
            booking.Status = "CheckedOut";

            if (!string.IsNullOrWhiteSpace(checkoutNote))
            {
                booking.Notes = string.IsNullOrWhiteSpace(booking.Notes)
                    ? checkoutNote.Trim()
                    : booking.Notes + " | Thanh toán: " + checkoutNote.Trim();
            }

            var room = db.Rooms.FirstOrDefault(r => r.Id == booking.RoomId);
            if (room != null)
            {
                room.Status = "Cleaning";
                room.Notes = "Khách " + booking.BookingCode + " vừa trả phòng lúc " + DateTime.Now.ToString("HH:mm dd/MM");

                int nextTaskId = db.HousekeepingTasks.Any() ? db.HousekeepingTasks.Max(t => t.Id) + 1 : 1;
                db.HousekeepingTasks.Add(new HousekeepingTask
                {
                    Id = nextTaskId,
                    RoomId = room.Id,
                    StaffName = "Cô Nguyễn Thị Lan",
                    TaskType = "Dọn sau trả phòng",
                    Priority = "Cao",
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    Notes = string.Format("Dọn phòng {0} sau khi khách {1} ({2}) trả phòng.",
                        room.RoomNumber, booking.CustomerName, booking.BookingCode)
                });
            }

            DataStore.SaveChanges();
            TempData["Success"] = string.Format(
                "Đã trả phòng & thanh toán đơn {0} thành công! Phòng {1} đã chuyển sang trạng thái Cần dọn vệ sinh.",
                booking.BookingCode, room != null ? room.RoomNumber : "");

            return RedirectToAction("Details", new { id = booking.Id });
        }

        private bool CanAccessBooking(Booking booking, UserAccount user)
        {
            if (user == null) return false;
            if (user.Role == "Admin" || user.Role == "Staff") return true;
            return booking.CustomerUserId == user.Id ||
                   (!string.IsNullOrEmpty(user.Phone) && booking.CustomerPhone == user.Phone);
        }

        private void PrepareRoomDropdown(int selectedRoomId)
        {
            var db = DataStore.Db;
            var roomTypes = db.RoomTypes.ToDictionary(rt => rt.Id, rt => rt);

            var roomItems = db.Rooms
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .Select(r =>
                {
                    var rt = roomTypes.ContainsKey(r.RoomTypeId) ? roomTypes[r.RoomTypeId] : null;
                    string label = string.Format("Phòng {0} - {1} ({2:N0}đ/đêm - Tối đa {3} khách)",
                        r.RoomNumber,
                        rt != null ? rt.Name : "",
                        rt != null ? rt.BasePrice : 0,
                        rt != null ? rt.MaxGuests : 2);
                    return new { r.Id, Label = label };
                }).ToList();

            ViewBag.RoomList = new SelectList(roomItems, "Id", "Label", selectedRoomId);
            ViewBag.RoomTypes = db.RoomTypes;
        }
    }
}
