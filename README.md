# Hệ Thống Quản Lý Đặt Phòng & Vận Hành Cơ Sở Lưu Trú (Homestay)

Hệ thống website đặt phòng trực tuyến và quản lý vận hành dành cho cơ sở lưu trú nhỏ (Homestay, Khách sạn mini, Nhà nghỉ) được xây dựng trên nền tảng **ASP.NET MVC 5 (.NET Framework 4.7.2)** và **Bootstrap 5**.

---

## 🌟 Tính năng chính theo 3 phân quyền (Actors)

### 1. Khách hàng (Customer)
- **Xem danh sách phòng trống:** Xem phòng còn trống theo từng danh mục loại phòng (Phòng đơn, phòng đôi, phòng gia đình, sân vườn...).
- **Xem chi tiết phòng:** Đơn giá, sức chứa tối đa, trang thiết bị tiện nghi và chính sách đặt cọc.
- **Đặt phòng & Đặt cọc (như sàn TMĐT):** Chọn khoảng ngày nhận/trả phòng, hệ thống tự động tính số đêm, đơn giá và số tiền đặt cọc tối thiểu (30%), kiểm tra chống trùng lịch tự động.
- **Quản lý đơn cá nhân:** Theo dõi lịch sử các đơn đặt phòng của mình (`/Booking/MyBookings`).
- **Đặt thêm dịch vụ phát sinh:** Đặt đồ ăn, đồ uống, thuê xe, giặt ủi... kèm ghi chú yêu cầu gửi đến nhân viên xác nhận.

### 2. Nhân viên (Staff)
- **Danh sách & Tra cứu phòng:** Tra cứu tình trạng phòng trống/đã kín lịch theo khoảng ngày bất kỳ.
- **Quản lý đặt phòng:** Theo dõi danh sách đơn đặt phòng, lọc theo tình trạng (Chờ nhận phòng, Đang lưu trú, Đã trả phòng, Đã hủy) hoặc tìm kiếm theo mã đơn, họ tên, số điện thoại.
- **Xác nhận Nhận phòng (Check-in):** Tiếp nhận khách check-in tại quầy lễ tân hoặc nhận phòng ngay khi đặt.
- **Xác nhận dịch vụ:** Duyệt các yêu cầu gọi dịch vụ của khách hàng hoặc bấm "Báo hết hàng" khi dịch vụ không khả dụng.
- **Thủ tục Trả phòng (Check-out) & Thanh toán:** Tự động tổng hợp tiền phòng, tiền dịch vụ đã duyệt, trừ tiền cọc, tính phụ thu/giảm giá và hiển thị số tiền còn lại khách cần thanh toán.
- **Quản lý dọn phòng (Housekeeping):** Khi khách trả phòng, phòng tự động chuyển sang trạng thái "Phòng đang chờ dọn vệ sinh". Nhân viên cập nhật tiến độ (Đang dọn -> Hoàn thành) để mở phòng trống đón khách mới.

### 3. Quản trị viên (Admin)
- **Quản lý cấu hình hệ thống (CRUD):**
  - **Quản lý loại phòng:** Thêm, sửa, xóa loại phòng và thiết lập bảng giá ngày thường, cuối tuần, phụ thu trả phòng muộn.
  - **Quản lý phòng:** Thêm, sửa, xóa phòng, phân tầng, điều chỉnh đơn giá và sức chứa.
  - **Quản lý dịch vụ:** Thêm, sửa, xóa danh mục dịch vụ phát sinh và đơn giá phục vụ.
  - **Quản lý tài khoản:** Tạo tài khoản, chỉnh sửa thông tin, phân quyền và khóa/mở khóa tài khoản người dùng.
- **Thống kê & Báo cáo doanh thu:**
  - Báo cáo theo khoảng ngày tùy chọn.
  - Thống kê tỷ lệ lấp đầy & doanh thu theo từng phòng.
  - Tổng hợp doanh thu theo loại phòng.
  - Báo cáo chi tiết doanh thu dịch vụ phát sinh.

---

## 🛠 Công nghệ sử dụng

- **Backend:** C#, ASP.NET MVC 5, .NET Framework 4.7.2
- **Frontend:** Razor View Engine (`.cshtml`), HTML5, CSS3, Bootstrap 5.2.3, JavaScript
- **Lưu trữ dữ liệu:** Dữ liệu tập trung tại `App_Data/homestay_data.json` thông qua lớp `DataStore` (Thread-safe lock, LINQ to Objects), không phụ thuộc vào SQL Server cục bộ, thuận tiện chạy demo ngay trên mọi máy tính.

---

## 🚀 Hướng dẫn cài đặt & Khởi chạy

1. **Yêu cầu môi trường:**
   - Visual Studio 2019 / 2022 (cài đặt workload *ASP.NET and web development*).
   - .NET Framework 4.7.2 Developer Pack.

2. **Cách chạy:**
   - Clone hoặc tải mã nguồn về máy:
     ```bash
     git clone https://github.com/haphuongnamvn-sys/QuanLyDatPhongHomestay.git
     ```
   - Mở file giải pháp `QuanLyLuuTru.csproj` hoặc `QuanLyLuuTru.slnx` bằng Visual Studio.
   - Nhấn **F5** hoặc **Ctrl + F5** (chạy với IIS Express).

---

## 🔑 Tài khoản thử nghiệm mặc định

| Vai trò | Tên đăng nhập | Mật khẩu | Chức năng chính |
| :--- | :--- | :--- | :--- |
| **Quản trị viên** | `quanly` | `123` | Cấu hình Thêm/Sửa/Xóa phòng, loại phòng, dịch vụ, tài khoản, thống kê doanh thu |
| **Nhân viên** | `nhanvien` | `123` | Tra cứu phòng, xác nhận nhận phòng, trả phòng, duyệt dịch vụ, quản lý dọn phòng |
| **Khách hàng** | `khachhang` | `123` | Xem phòng trống, đặt phòng + đặt cọc, đặt thêm dịch vụ, quản lý đơn cá nhân |

*(Khách hàng cũng có thể bấm **Đăng ký** trên website để tạo tài khoản mới)*
