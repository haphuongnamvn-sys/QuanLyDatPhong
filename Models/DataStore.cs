using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace QuanLyLuuTru.Models
{
    /// <summary>
    /// Cấu trúc lưu trữ dữ liệu của hệ thống (lưu vào App_Data/homestay_data.json)
    /// </summary>
    public class DatabaseContainer
    {
        public List<UserAccount> Users { get; set; } = new List<UserAccount>();
        public List<RoomType> RoomTypes { get; set; } = new List<RoomType>();
        public List<Room> Rooms { get; set; } = new List<Room>();
        public List<ServiceItem> Services { get; set; } = new List<ServiceItem>();
        public List<Booking> Bookings { get; set; } = new List<Booking>();
        public List<BookingService> BookingServices { get; set; } = new List<BookingService>();
        public List<HousekeepingTask> HousekeepingTasks { get; set; } = new List<HousekeepingTask>();
    }

    public static class DataStore
    {
        private static readonly object _lock = new object();
        private static DatabaseContainer _db;

        private static string GetFilePath()
        {
            if (HttpContext.Current != null && HttpContext.Current.Server != null)
            {
                string folder = HttpContext.Current.Server.MapPath("~/App_Data");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                return Path.Combine(folder, "homestay_data.json");
            }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "homestay_data.json");
        }

        public static DatabaseContainer Db
        {
            get
            {
                lock (_lock)
                {
                    if (_db == null)
                    {
                        LoadData();
                    }
                    return _db;
                }
            }
        }

        public static void LoadData()
        {
            lock (_lock)
            {
                string path = GetFilePath();
                if (File.Exists(path))
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        _db = JsonConvert.DeserializeObject<DatabaseContainer>(json);
                    }
                    catch
                    {
                        _db = null;
                    }
                }

                if (_db == null || _db.RoomTypes == null || _db.RoomTypes.Count == 0 || _db.Users == null || _db.Users.Count == 0 || _db.Rooms.Any(r => r.PricePerNight <= 0))
                {
                    _db = CreateSeedData();
                    SaveChanges();
                }
            }
        }

        public static void SaveChanges()
        {
            lock (_lock)
            {
                if (_db == null) return;
                string path = GetFilePath();
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = JsonConvert.SerializeObject(_db, Formatting.Indented);
                File.WriteAllText(path, json);
            }
        }

        public static void ResetToSeedData()
        {
            lock (_lock)
            {
                _db = CreateSeedData();
                SaveChanges();
            }
        }

        public static int CalculateNights(DateTime checkIn, DateTime checkOut)
        {
            int nights = (checkOut.Date - checkIn.Date).Days;
            return nights <= 0 ? 1 : nights;
        }

        public static decimal CalculateRoomCharge(Room room, DateTime checkIn, DateTime checkOut)
        {
            if (room == null) return 0;
            int nights = CalculateNights(checkIn, checkOut);
            return nights * room.PricePerNight;
        }

        public static decimal CalculateRoomCharge(RoomType roomType, DateTime checkIn, DateTime checkOut)
        {
            if (roomType == null) return 0;
            int nights = CalculateNights(checkIn, checkOut);
            return nights * roomType.BasePrice;
        }

        public static Booking GetConflictingBooking(int roomId, DateTime checkIn, DateTime checkOut, int? ignoreBookingId = null)
        {
            DateTime start = checkIn.Date;
            DateTime end = checkOut.Date <= start ? start.AddDays(1) : checkOut.Date;

            return Db.Bookings.FirstOrDefault(b =>
                b.RoomId == roomId &&
                b.Status != "Cancelled" &&
                b.Status != "CheckedOut" &&
                (!ignoreBookingId.HasValue || b.Id != ignoreBookingId.Value) &&
                b.CheckInDate.Date < end &&
                start < b.CheckOutDate.Date
            );
        }

        public static bool IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut, int? ignoreBookingId = null)
        {
            var room = Db.Rooms.FirstOrDefault(r => r.Id == roomId);
            if (room == null || room.Status == "Maintenance")
            {
                return false;
            }
            return GetConflictingBooking(roomId, checkIn, checkOut, ignoreBookingId) == null;
        }

        public static void RecalculateBookingServiceTotal(int bookingId)
        {
            var booking = Db.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking != null)
            {
                booking.ServiceTotalAmount = Db.BookingServices
                    .Where(s => s.BookingId == bookingId && (s.Status == "Confirmed" || string.IsNullOrEmpty(s.Status)))
                    .Sum(s => s.Quantity * s.UnitPrice);
                SaveChanges();
            }
        }

        private static DatabaseContainer CreateSeedData()
        {
            DateTime today = DateTime.Today;
            var data = new DatabaseContainer();

            // 0. Danh sách Tài khoản & Phân quyền (3 nhóm Actor: Admin, Staff, Customer)
            data.Users = new List<UserAccount>
            {
                new UserAccount
                {
                    Id = 1,
                    Username = "quanly",
                    Password = "123",
                    FullName = "Nguyễn Văn Quản Trị",
                    Phone = "0901112222",
                    Email = "admin@homestay.vn",
                    IdentityCard = "001090001111",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = today.AddDays(-30)
                },
                new UserAccount
                {
                    Id = 2,
                    Username = "nhanvien",
                    Password = "123",
                    FullName = "Trần Minh (Nhân viên)",
                    Phone = "0903334444",
                    Email = "nhanvien@homestay.vn",
                    IdentityCard = "001095002222",
                    Role = "Staff",
                    IsActive = true,
                    CreatedAt = today.AddDays(-25)
                },
                new UserAccount
                {
                    Id = 3,
                    Username = "colan",
                    Password = "123",
                    FullName = "Cô Nguyễn Thị Lan",
                    Phone = "0905556666",
                    Email = "colan@homestay.vn",
                    IdentityCard = "001085003333",
                    Role = "Staff",
                    IsActive = true,
                    CreatedAt = today.AddDays(-20)
                },
                new UserAccount
                {
                    Id = 4,
                    Username = "khachhang",
                    Password = "123",
                    FullName = "Hoàng Bích Phương",
                    Phone = "0945678901",
                    Email = "bichphuong@gmail.com",
                    IdentityCard = "015196007788",
                    Role = "Customer",
                    IsActive = true,
                    CreatedAt = today.AddDays(-10)
                },
                new UserAccount
                {
                    Id = 5,
                    Username = "khachhang2",
                    Password = "123",
                    FullName = "Phạm Quốc Khánh",
                    Phone = "0977112233",
                    Email = "quockhanh@gmail.com",
                    IdentityCard = "038092001122",
                    Role = "Customer",
                    IsActive = true,
                    CreatedAt = today.AddDays(-8)
                }
            };

            // 1. Bảng giá & Loại phòng
            data.RoomTypes = new List<RoomType>
            {
                new RoomType
                {
                    Id = 1,
                    Name = "Phòng Đơn Tiêu Chuẩn",
                    BasePrice = 300000,
                    WeekendPrice = 350000,
                    LateCheckoutFeePerHour = 50000,
                    MaxGuests = 2,
                    Description = "Giường đôi 1m6, điều hòa, tivi, bình nóng lạnh, wifi"
                },
                new RoomType
                {
                    Id = 2,
                    Name = "Phòng Đôi Ban Công",
                    BasePrice = 450000,
                    WeekendPrice = 520000,
                    LateCheckoutFeePerHour = 70000,
                    MaxGuests = 2,
                    Description = "Giường 1m8, có ban công thoáng mát, bàn trà, tủ lạnh nhỏ"
                },
                new RoomType
                {
                    Id = 3,
                    Name = "Phòng Gia Đình 2 Giường",
                    BasePrice = 650000,
                    WeekendPrice = 750000,
                    LateCheckoutFeePerHour = 100000,
                    MaxGuests = 4,
                    Description = "2 giường lớn 1m8, phòng rộng 35m2, phù hợp gia đình hoặc nhóm 4 người"
                },
                new RoomType
                {
                    Id = 4,
                    Name = "Phòng Sân Vườn Riêng",
                    BasePrice = 800000,
                    WeekendPrice = 950000,
                    LateCheckoutFeePerHour = 120000,
                    MaxGuests = 4,
                    Description = "Nhà gỗ sân vườn riêng biệt, có sân nướng BBQ, không gian yên tĩnh"
                }
            };

            // 2. Danh sách 10 phòng (Gộp trực tiếp loại phòng, giá phòng, sức chứa, tiện nghi vào từng phòng)
            data.Rooms = new List<Room>
            {
                new Room { Id = 1, RoomNumber = "P101", Floor = 1, RoomTypeId = 1, CategoryName = "Phòng Đơn Tiêu Chuẩn", PricePerNight = 300000, MaxGuests = 2, Description = "Giường đôi 1m6, điều hòa, tivi, bình nóng lạnh, wifi", Status = "Occupied", Notes = "Tầng 1 gần quầy lễ tân" },
                new Room { Id = 2, RoomNumber = "P102", Floor = 1, RoomTypeId = 1, CategoryName = "Phòng Đơn Tiêu Chuẩn", PricePerNight = 300000, MaxGuests = 2, Description = "Giường đôi 1m6, điều hòa, tivi, bình nóng lạnh, wifi", Status = "Available", Notes = "Phòng yên tĩnh" },
                new Room { Id = 3, RoomNumber = "P103", Floor = 1, RoomTypeId = 1, CategoryName = "Phòng Đơn Tiêu Chuẩn", PricePerNight = 300000, MaxGuests = 2, Description = "Giường đôi 1m6, điều hòa, tivi, bình nóng lạnh, wifi", Status = "Cleaning", Notes = "Khách vừa trả phòng sáng nay" },
                new Room { Id = 4, RoomNumber = "P201", Floor = 2, RoomTypeId = 2, CategoryName = "Phòng Đôi Ban Công", PricePerNight = 450000, MaxGuests = 2, Description = "Giường 1m8, có ban công thoáng mát, bàn trà, tủ lạnh nhỏ", Status = "Occupied", Notes = "Ban công hướng phố" },
                new Room { Id = 5, RoomNumber = "P202", Floor = 2, RoomTypeId = 2, CategoryName = "Phòng Đôi Ban Công", PricePerNight = 450000, MaxGuests = 2, Description = "Giường 1m8, có ban công thoáng mát, bàn trà, tủ lạnh nhỏ", Status = "Available", Notes = "Phòng sạch sẽ sẵn sàng" },
                new Room { Id = 6, RoomNumber = "P203", Floor = 2, RoomTypeId = 2, CategoryName = "Phòng Đôi Ban Công", PricePerNight = 450000, MaxGuests = 2, Description = "Giường 1m8, có ban công thoáng mát, bàn trà, tủ lạnh nhỏ", Status = "Available", Notes = "Ban công góc thoáng" },
                new Room { Id = 7, RoomNumber = "P301", Floor = 3, RoomTypeId = 3, CategoryName = "Phòng Gia Đình 2 Giường", PricePerNight = 650000, MaxGuests = 4, Description = "2 giường lớn 1m8, phòng rộng 35m2, phù hợp gia đình hoặc nhóm 4 người", Status = "Occupied", Notes = "Gia đình đang ở" },
                new Room { Id = 8, RoomNumber = "P302", Floor = 3, RoomTypeId = 3, CategoryName = "Phòng Gia Đình 2 Giường", PricePerNight = 650000, MaxGuests = 4, Description = "2 giường lớn 1m8, phòng rộng 35m2, phù hợp gia đình hoặc nhóm 4 người", Status = "Available", Notes = "Phòng rộng tầng 3" },
                new Room { Id = 9, RoomNumber = "SV01", Floor = 1, RoomTypeId = 4, CategoryName = "Phòng Sân Vườn Riêng", PricePerNight = 800000, MaxGuests = 4, Description = "Nhà gỗ sân vườn riêng biệt, có sân nướng BBQ, không gian yên tĩnh", Status = "Available", Notes = "Khu sân vườn" },
                new Room { Id = 10, RoomNumber = "SV02", Floor = 1, RoomTypeId = 4, CategoryName = "Phòng Sân Vườn Riêng", PricePerNight = 800000, MaxGuests = 4, Description = "Nhà gỗ sân vườn riêng biệt, có sân nướng BBQ, không gian yên tĩnh", Status = "Maintenance", Notes = "Đang sửa điều hòa" }
            };

            // 3. Danh mục dịch vụ
            data.Services = new List<ServiceItem>
            {
                new ServiceItem { Id = 1, Name = "Nước suối Aquafina", Category = "Đồ uống", Unit = "Chai", Price = 15000, IsActive = true },
                new ServiceItem { Id = 2, Name = "Nước ngọt Coca / RedBull", Category = "Đồ uống", Unit = "Lon", Price = 25000, IsActive = true },
                new ServiceItem { Id = 3, Name = "Mì ly xúc xích", Category = "Ăn uống", Unit = "Ly", Price = 25000, IsActive = true },
                new ServiceItem { Id = 4, Name = "Ăn sáng (Bún / Phở + Cafe)", Category = "Ăn uống", Unit = "Suất", Price = 65000, IsActive = true },
                new ServiceItem { Id = 5, Name = "Giặt sấy quần áo", Category = "Tiện ích", Unit = "Kg", Price = 40000, IsActive = true },
                new ServiceItem { Id = 6, Name = "Thuê xe máy theo ngày", Category = "Tiện ích", Unit = "Ngày", Price = 150000, IsActive = true },
                new ServiceItem { Id = 7, Name = "Thuê bếp nướng BBQ sân vườn", Category = "Tiện ích", Unit = "Bộ", Price = 200000, IsActive = true },
                new ServiceItem { Id = 8, Name = "Phụ thu thêm người", Category = "Phụ thu", Unit = "Người", Price = 100000, IsActive = true }
            };

            // 4. Dữ liệu đặt phòng mẫu
            data.Bookings = new List<Booking>
            {
                new Booking
                {
                    Id = 1,
                    BookingCode = "DP001",
                    CustomerUserId = null,
                    CustomerName = "Nguyễn Văn Hùng",
                    CustomerPhone = "0904123456",
                    CustomerIdentityCard = "001095012345",
                    GuestCount = 2,
                    RoomId = 2,
                    CheckInDate = today.AddDays(-6),
                    CheckOutDate = today.AddDays(-3),
                    ActualCheckIn = today.AddDays(-6).AddHours(14),
                    ActualCheckOut = today.AddDays(-3).AddHours(11.5),
                    RoomPricePerNight = 300000,
                    TotalNights = 3,
                    RoomTotalAmount = 950000,
                    ServiceTotalAmount = 190000,
                    ExtraCharge = 0,
                    DiscountAmount = 0,
                    DepositAmount = 300000,
                    Status = "CheckedOut",
                    Notes = "Khách ở 3 đêm, đã thanh toán đủ",
                    CreatedAt = today.AddDays(-8)
                },
                new Booking
                {
                    Id = 2,
                    BookingCode = "DP002",
                    CustomerUserId = null,
                    CustomerName = "Trần Thị Minh Thư",
                    CustomerPhone = "0988765432",
                    CustomerIdentityCard = "036198004521",
                    GuestCount = 4,
                    RoomId = 9,
                    CheckInDate = today.AddDays(-3),
                    CheckOutDate = today.AddDays(-1),
                    ActualCheckIn = today.AddDays(-3).AddHours(14),
                    ActualCheckOut = today.AddDays(-1).AddHours(12),
                    RoomPricePerNight = 800000,
                    TotalNights = 2,
                    RoomTotalAmount = 1750000,
                    ServiceTotalAmount = 460000,
                    ExtraCharge = 120000,
                    DiscountAmount = 50000,
                    DepositAmount = 500000,
                    Status = "CheckedOut",
                    Notes = "Thuê bếp nướng BBQ, trả phòng muộn 1 giờ",
                    CreatedAt = today.AddDays(-5)
                },
                new Booking
                {
                    Id = 3,
                    BookingCode = "DP003",
                    CustomerUserId = null,
                    CustomerName = "Lê Hoàng Nam",
                    CustomerPhone = "0912345678",
                    CustomerIdentityCard = "001099008877",
                    GuestCount = 2,
                    RoomId = 3,
                    CheckInDate = today.AddDays(-2),
                    CheckOutDate = today,
                    ActualCheckIn = today.AddDays(-2).AddHours(13.5),
                    ActualCheckOut = today.AddHours(9.5),
                    RoomPricePerNight = 300000,
                    TotalNights = 2,
                    RoomTotalAmount = 600000,
                    ServiceTotalAmount = 80000,
                    ExtraCharge = 0,
                    DiscountAmount = 0,
                    DepositAmount = 200000,
                    Status = "CheckedOut",
                    Notes = "Đã trả phòng lúc 09:30 sáng nay",
                    CreatedAt = today.AddDays(-3)
                },
                new Booking
                {
                    Id = 4,
                    BookingCode = "DP004",
                    CustomerUserId = 5, // Phạm Quốc Khánh
                    CustomerName = "Phạm Quốc Khánh",
                    CustomerPhone = "0977112233",
                    CustomerIdentityCard = "038092001122",
                    GuestCount = 2,
                    RoomId = 1,
                    CheckInDate = today.AddDays(-2),
                    CheckOutDate = today.AddDays(1),
                    ActualCheckIn = today.AddDays(-2).AddHours(14),
                    ActualCheckOut = null,
                    RoomPricePerNight = 300000,
                    TotalNights = 3,
                    RoomTotalAmount = 900000,
                    ServiceTotalAmount = 205000,
                    ExtraCharge = 0,
                    DiscountAmount = 0,
                    DepositAmount = 300000,
                    Status = "CheckedIn",
                    Notes = "Khách đang ở, có thuê xe máy",
                    CreatedAt = today.AddDays(-3)
                },
                new Booking
                {
                    Id = 5,
                    BookingCode = "DP005",
                    CustomerUserId = null,
                    CustomerName = "Vũ Ngọc Ánh",
                    CustomerPhone = "0933445566",
                    CustomerIdentityCard = "001197003344",
                    GuestCount = 2,
                    RoomId = 4,
                    CheckInDate = today.AddDays(-1),
                    CheckOutDate = today.AddDays(2),
                    ActualCheckIn = today.AddDays(-1).AddHours(14.5),
                    ActualCheckOut = null,
                    RoomPricePerNight = 450000,
                    TotalNights = 3,
                    RoomTotalAmount = 1350000,
                    ServiceTotalAmount = 130000,
                    ExtraCharge = 0,
                    DiscountAmount = 0,
                    DepositAmount = 500000,
                    Status = "CheckedIn",
                    Notes = "Đặt thêm 2 suất ăn sáng",
                    CreatedAt = today.AddDays(-2)
                },
                new Booking
                {
                    Id = 6,
                    BookingCode = "DP006",
                    CustomerUserId = null,
                    CustomerName = "Đặng Minh Tuấn",
                    CustomerPhone = "0966889900",
                    CustomerIdentityCard = "024088005566",
                    GuestCount = 4,
                    RoomId = 7,
                    CheckInDate = today,
                    CheckOutDate = today.AddDays(2),
                    ActualCheckIn = today.AddHours(12.5),
                    ActualCheckOut = null,
                    RoomPricePerNight = 650000,
                    TotalNights = 2,
                    RoomTotalAmount = 1300000,
                    ServiceTotalAmount = 90000,
                    ExtraCharge = 0,
                    DiscountAmount = 0,
                    DepositAmount = 500000,
                    Status = "CheckedIn",
                    Notes = "Gia đình 4 người vừa nhận phòng",
                    CreatedAt = today.AddDays(-4)
                },
                new Booking
                {
                    Id = 7,
                    BookingCode = "DP007",
                    CustomerUserId = 4, // Hoàng Bích Phương
                    CustomerName = "Hoàng Bích Phương",
                    CustomerPhone = "0945678901",
                    CustomerIdentityCard = "015196007788",
                    GuestCount = 2,
                    RoomId = 5,
                    CheckInDate = today,
                    CheckOutDate = today.AddDays(2),
                    ActualCheckIn = null,
                    ActualCheckOut = null,
                    RoomPricePerNight = 450000,
                    TotalNights = 2,
                    RoomTotalAmount = 900000,
                    ServiceTotalAmount = 0,
                    ExtraCharge = 0,
                    DiscountAmount = 0,
                    DepositAmount = 300000,
                    Status = "Booked",
                    Notes = "Khách đặt online, báo 16h chiều nay nhận phòng",
                    CreatedAt = today.AddDays(-1)
                }
            };

            // 5. Dịch vụ phát sinh mẫu
            data.BookingServices = new List<BookingService>
            {
                new BookingService { Id = 1, BookingId = 1, ServiceId = 6, ServiceName = "Thuê xe máy theo ngày", Unit = "Ngày", Quantity = 1, UnitPrice = 150000, CreatedAt = today.AddDays(-5), Note = "Thuê 1 ngày" },
                new BookingService { Id = 2, BookingId = 1, ServiceId = 5, ServiceName = "Giặt sấy quần áo", Unit = "Kg", Quantity = 1, UnitPrice = 40000, CreatedAt = today.AddDays(-4), Note = "Giặt đồ" },
                new BookingService { Id = 3, BookingId = 2, ServiceId = 7, ServiceName = "Thuê bếp nướng BBQ sân vườn", Unit = "Bộ", Quantity = 1, UnitPrice = 200000, CreatedAt = today.AddDays(-2), Note = "Tiệc tối" },
                new BookingService { Id = 4, BookingId = 2, ServiceId = 4, ServiceName = "Ăn sáng (Bún / Phở + Cafe)", Unit = "Suất", Quantity = 4, UnitPrice = 65000, CreatedAt = today.AddDays(-2), Note = "4 suất sáng" },
                new BookingService { Id = 5, BookingId = 3, ServiceId = 1, ServiceName = "Nước suối Aquafina", Unit = "Chai", Quantity = 2, UnitPrice = 15000, CreatedAt = today.AddDays(-1), Note = "" },
                new BookingService { Id = 6, BookingId = 3, ServiceId = 3, ServiceName = "Mì ly xúc xích", Unit = "Ly", Quantity = 2, UnitPrice = 25000, CreatedAt = today.AddDays(-1), Note = "" },
                new BookingService { Id = 7, BookingId = 4, ServiceId = 6, ServiceName = "Thuê xe máy theo ngày", Unit = "Ngày", Quantity = 1, UnitPrice = 150000, CreatedAt = today.AddDays(-1), Note = "" },
                new BookingService { Id = 8, BookingId = 4, ServiceId = 1, ServiceName = "Nước suối Aquafina", Unit = "Chai", Quantity = 2, UnitPrice = 15000, CreatedAt = today.AddDays(-1), Note = "" },
                new BookingService { Id = 9, BookingId = 4, ServiceId = 2, ServiceName = "Nước ngọt Coca / RedBull", Unit = "Lon", Quantity = 1, UnitPrice = 25000, CreatedAt = today, Note = "" },
                new BookingService { Id = 10, BookingId = 5, ServiceId = 4, ServiceName = "Ăn sáng (Bún / Phở + Cafe)", Unit = "Suất", Quantity = 2, UnitPrice = 65000, CreatedAt = today, Note = "" },
                new BookingService { Id = 11, BookingId = 6, ServiceId = 1, ServiceName = "Nước suối Aquafina", Unit = "Chai", Quantity = 4, UnitPrice = 15000, CreatedAt = today, Note = "", Status = "Confirmed" },
                new BookingService { Id = 12, BookingId = 6, ServiceId = 5, ServiceName = "Giặt sấy quần áo", Unit = "Kg", Quantity = 1, UnitPrice = 30000, CreatedAt = today, Note = "", Status = "Confirmed" },
                new BookingService { Id = 13, BookingId = 7, ServiceId = 4, ServiceName = "Ăn sáng (Bún / Phở + Cafe)", Unit = "Suất", Quantity = 2, UnitPrice = 65000, CreatedAt = today, Note = "Khách đặt trước 2 suất sáng mai", Status = "Pending" },
                new BookingService { Id = 14, BookingId = 4, ServiceId = 2, ServiceName = "Nước ngọt Coca / RedBull", Unit = "Lon", Quantity = 2, UnitPrice = 25000, CreatedAt = today, Note = "Mang lên phòng P101", Status = "Pending" }
            };

            // 6. Công việc dọn phòng
            data.HousekeepingTasks = new List<HousekeepingTask>
            {
                new HousekeepingTask
                {
                    Id = 1,
                    RoomId = 3,
                    StaffName = "Cô Nguyễn Thị Lan",
                    TaskType = "Dọn sau trả phòng",
                    Priority = "Gấp",
                    Status = "Pending",
                    CreatedAt = today.AddHours(9.5),
                    CompletedAt = null,
                    Notes = "Khách DP003 vừa trả phòng, cần thay chăn ga mới"
                },
                new HousekeepingTask
                {
                    Id = 2,
                    RoomId = 4,
                    StaffName = "Cô Nguyễn Thị Lan",
                    TaskType = "Dọn phòng định kỳ",
                    Priority = "Bình thường",
                    Status = "InProgress",
                    CreatedAt = today.AddHours(10),
                    CompletedAt = null,
                    Notes = "Thay khăn tắm và thêm 2 chai nước"
                },
                new HousekeepingTask
                {
                    Id = 3,
                    RoomId = 9,
                    StaffName = "Cô Nguyễn Thị Lan",
                    TaskType = "Dọn sau trả phòng",
                    Priority = "Cao",
                    Status = "Completed",
                    CreatedAt = today.AddDays(-1).AddHours(12),
                    CompletedAt = today.AddDays(-1).AddHours(13.5),
                    Notes = "Đã dọn sạch sẽ"
                }
            };

            return data;
        }
    }
}
