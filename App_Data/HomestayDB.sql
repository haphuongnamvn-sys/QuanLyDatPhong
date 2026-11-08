-- =============================================================================
-- KỊCH BẢN CƠ SỞ DỮ LIỆU: HỆ THỐNG QUẢN LÝ ĐẶT PHÒNG & VẬN HÀNH CƠ SỞ LƯU TRÚ NHỎ
-- Môn học / Đồ án: Phát triển phần mềm ASP.NET MVC 5
-- =============================================================================

CREATE DATABASE HomestayManagementDB;
GO
USE HomestayManagementDB;
GO

-- 1. Bảng Hạng Phòng & Bảng Giá (RoomTypes)
CREATE TABLE RoomTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    BasePrice DECIMAL(18,0) NOT NULL,          -- Giá ngày thường (VNĐ/đêm)
    WeekendPrice DECIMAL(18,0) NOT NULL,       -- Giá cuối tuần T7, CN (VNĐ/đêm)
    LateCheckoutFeePerHour DECIMAL(18,0) DEFAULT 50000,
    MaxGuests INT NOT NULL DEFAULT 2,
    Description NVARCHAR(500)
);

-- 2. Bảng Phòng Lưu Trú (Rooms)
CREATE TABLE Rooms (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoomNumber NVARCHAR(20) NOT NULL UNIQUE,   -- Số phòng: P101, P102...
    Floor INT NOT NULL DEFAULT 1,
    RoomTypeId INT NOT NULL FOREIGN KEY REFERENCES RoomTypes(Id),
    Status NVARCHAR(30) NOT NULL DEFAULT 'Available', -- Available, Occupied, Cleaning, Maintenance
    Notes NVARCHAR(300)
);

-- 3. Bảng Danh Mục Dịch Vụ Phát Sinh (Services)
CREATE TABLE Services (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Category NVARCHAR(50),
    Unit NVARCHAR(30),
    Price DECIMAL(18,0) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

-- 4. Bảng Đặt Phòng & Lưu Trú (Bookings)
CREATE TABLE Bookings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BookingCode NVARCHAR(20) NOT NULL UNIQUE,
    CustomerName NVARCHAR(120) NOT NULL,
    CustomerPhone NVARCHAR(20) NOT NULL,
    CustomerIdentityCard NVARCHAR(30),
    GuestCount INT NOT NULL DEFAULT 1,
    RoomId INT NOT NULL FOREIGN KEY REFERENCES Rooms(Id),
    CheckInDate DATE NOT NULL,
    CheckOutDate DATE NOT NULL,
    ActualCheckIn DATETIME NULL,
    ActualCheckOut DATETIME NULL,
    RoomPricePerNight DECIMAL(18,0) NOT NULL,
    TotalNights INT NOT NULL,
    RoomTotalAmount DECIMAL(18,0) NOT NULL,
    ServiceTotalAmount DECIMAL(18,0) NOT NULL DEFAULT 0,
    ExtraCharge DECIMAL(18,0) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,0) NOT NULL DEFAULT 0,
    DepositAmount DECIMAL(18,0) NOT NULL DEFAULT 0,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Booked', -- Booked, CheckedIn, CheckedOut, Cancelled
    Notes NVARCHAR(500),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- 5. Bảng Chi Tiết Dịch Vụ Phát Sinh Theo Đơn Đặt Phòng (BookingServices)
CREATE TABLE BookingServices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL FOREIGN KEY REFERENCES Bookings(Id),
    ServiceId INT NOT NULL FOREIGN KEY REFERENCES Services(Id),
    ServiceName NVARCHAR(150) NOT NULL,
    Unit NVARCHAR(30),
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,0) NOT NULL,
    Note NVARCHAR(250),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- 6. Bảng Công Việc Vệ Sinh Phòng (HousekeepingTasks)
CREATE TABLE HousekeepingTasks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoomId INT NOT NULL FOREIGN KEY REFERENCES Rooms(Id),
    StaffName NVARCHAR(100) NOT NULL,
    TaskType NVARCHAR(100) NOT NULL,
    Priority NVARCHAR(50) NOT NULL DEFAULT N'Bình thường',
    Status NVARCHAR(30) NOT NULL DEFAULT 'Pending', -- Pending, InProgress, Completed
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CompletedAt DATETIME NULL,
    Notes NVARCHAR(400)
);
GO
