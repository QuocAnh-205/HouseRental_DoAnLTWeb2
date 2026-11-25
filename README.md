# House Rental API — Backend hệ thống cho thuê nhà

ASP.NET Core Web API (.NET 10) theo kiến trúc phân lớp **Controller → Service → Repository → DbContext**, dùng EF Core + SQL Server, xác thực JWT, phân quyền theo vai trò.

## Công nghệ

| Thành phần | Thư viện |
|---|---|
| Framework | ASP.NET Core 10 (Controllers) |
| ORM | Entity Framework Core 10 – SQL Server, EF Migrations |
| Xác thực | JWT Bearer + Refresh token (xoay vòng) |
| Mã hóa mật khẩu | BCrypt.Net-Next |
| Tài liệu API | Microsoft.AspNetCore.OpenApi + Swagger UI + Scalar UI |
| Tác vụ nền | `BackgroundService` (hết hạn hợp đồng, hóa đơn quá hạn, nhắc lịch hẹn) |

## Chức năng

| Nhóm | Chức năng |
|---|---|
| Tài khoản | Đăng ký, đăng nhập, refresh token, đăng xuất / đăng xuất mọi thiết bị, quên & đặt lại mật khẩu, đổi mật khẩu, hồ sơ cá nhân, hồ sơ công khai |
| Tin đăng | Đăng tin chờ duyệt, sửa / xóa / ẩn / hiện, duyệt / từ chối kèm lý do, ảnh (thêm, xóa, đặt ảnh đại diện), upload ảnh, tọa độ bản đồ, lượt xem, nhà tương tự |
| Tìm kiếm | Từ khóa, thành phố / quận / phường, loại nhà, khoảng giá, diện tích, số phòng ngủ / tắm, tiện ích, tìm quanh vị trí theo bán kính, sắp xếp (mới nhất, giá, diện tích, đánh giá, lượt xem, khoảng cách) |
| Yêu thích | Lưu / bỏ lưu nhà, danh sách nhà đã lưu, cờ `isFavorited` trong chi tiết nhà |
| Lịch hẹn xem nhà | Đặt lịch, chủ nhà xác nhận / từ chối, hủy (hai phía), hoàn tất, chống trùng lịch, nhắc trước 24 giờ |
| Thuê nhà | Gửi yêu cầu thuê, duyệt (tự từ chối các yêu cầu trùng), từ chối, hủy, kết thúc hợp đồng, tự kết thúc khi hết hạn |
| Hóa đơn tháng | Tiền thuê + điện + nước (theo chỉ số) + dịch vụ + phí khác, tự lấy chỉ số cũ từ hóa đơn trước, hạn thanh toán, trả nhiều lần, tự chuyển quá hạn |
| Thanh toán | Tiền cọc / tiền thuê / theo hóa đơn, chủ nhà xác nhận, hoàn tiền, lịch sử của người thuê và chủ nhà |
| Đánh giá | Chỉ người đã thuê được đánh giá, sửa đánh giá, chủ nhà phản hồi, tự tính điểm trung bình |
| Tin nhắn | Trò chuyện người thuê ↔ chủ nhà (gắn với căn nhà), số tin chưa đọc, tự đánh dấu đã đọc |
| Thông báo | Tự sinh theo mọi sự kiện nghiệp vụ, lọc, đếm chưa đọc, đánh dấu đã đọc, xóa |
| Báo cáo vi phạm | Người dùng báo cáo tin, admin xử lý (chấp nhận / bác bỏ, có thể ẩn tin) |
| Thống kê | Dashboard cho Admin, Landlord (doanh thu 12 tháng, công nợ, lượt xem...) và Tenant |
| Quản trị | Quản lý người dùng (khóa / mở khóa, đổi vai trò), duyệt tin, xem toàn bộ hợp đồng |

## Cấu trúc thư mục

```
HouseRentalAPI
├── Controllers/            # Nhận request, trả ApiResponse
├── Models/                 # Entity ánh xạ bảng CSDL
├── DTOs/                   # Dữ liệu vào/ra API (có validate)
├── Services/               # Nghiệp vụ (interface + implementation), tác vụ nền
├── Repositories/           # Truy cập dữ liệu (generic + riêng từng entity)
├── Data/
│   ├── AppDbContext.cs
│   ├── DbSeeder.cs
│   ├── Configurations/     # Fluent API cho từng entity
│   └── Migrations/         # EF Core migrations
├── Middleware/             # ExceptionMiddleware
├── Exceptions/             # NotFound, BadRequest, Forbidden, Conflict...
├── Mappings/               # Entity -> DTO
├── Extensions/             # Đăng ký DI, JWT, CORS, OpenAPI, phân trang
├── Constants/              # Role, claim type
├── Settings/               # JwtSettings (Options pattern)
├── wwwroot/uploads/        # Ảnh upload
└── Program.cs
```

## Thiết kế CSDL

```
Users 1 ──< Properties 1 ──< PropertyImages
  │              │
  │              ├──< Bookings 1 ──< Invoices 1 ──< Payments
  │              │       │                              ▲
  │              │       └──────────────────────────────┘
  │              ├──< Reviews        (TenantId)
  │              ├──< Appointments   (TenantId)
  │              ├──< Reports        (ReporterId)
  │              └──< Favorites      (UserId)
  │
  ├──< Conversations (Initiator/Recipient, PropertyId?) 1 ──< Messages
  ├──< Notifications
  └──< RefreshTokens
```

- **Properties**: `PendingApproval → Available ⇄ Hidden`, `Rented`, `Rejected` (kèm `RejectReason`). Lưu sẵn `AverageRating`, `ReviewCount`, `ViewCount`.
- **Bookings**: chốt giá khi đặt; `Pending → Approved → Completed`, hoặc `Rejected` / `Cancelled`.
- **Invoices**: mỗi hợp đồng 1 hóa đơn / tháng; `Unpaid → PartiallyPaid → Paid`, `Overdue`, `Cancelled`.
- **Payments**: `Pending → Success | Failed`, `Success → Refunded`; có thể gắn với một hóa đơn.
- **Appointments**: `Pending → Confirmed → Completed`, hoặc `Rejected` / `Cancelled`.
- **Reports**: `Pending → Resolved | Dismissed`.
- **RefreshTokens**: chỉ lưu hash SHA-256; mã đặt lại mật khẩu cũng chỉ lưu hash.

## Danh sách API

### Tài khoản — `/api/auth`, `/api/users`

| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Đăng ký |
| POST | `/api/auth/login` | Public | Đăng nhập, nhận access + refresh token |
| POST | `/api/auth/refresh` | Public | Đổi refresh token lấy cặp token mới |
| POST | `/api/auth/logout` | Public | Thu hồi refresh token |
| POST | `/api/auth/logout-all` | Đăng nhập | Đăng xuất mọi thiết bị |
| POST | `/api/auth/forgot-password` | Public | Gửi mã đặt lại mật khẩu (Development trả mã trong response) |
| POST | `/api/auth/reset-password` | Public | Đặt lại mật khẩu |
| GET | `/api/auth/me` | Đăng nhập | Thông tin của tôi |
| PUT | `/api/auth/change-password` | Đăng nhập | Đổi mật khẩu |
| GET | `/api/users` | Admin | Danh sách người dùng |
| GET | `/api/users/{id}` | Admin / chính mình | Chi tiết người dùng |
| GET | `/api/users/{id}/profile` | Public | Hồ sơ công khai |
| GET | `/api/users/{id}/properties` | Public | Tin đang cho thuê của chủ nhà |
| GET | `/api/users/{id}/posts` | Public | Bản tin trang cá nhân (chính chủ / Admin thấy cả tin chờ duyệt, bị từ chối, đã ẩn) |
| PUT | `/api/users/me` | Đăng nhập | Cập nhật hồ sơ |
| PATCH | `/api/users/{id}/status` | Admin | Khóa / mở khóa |
| PATCH | `/api/users/{id}/role` | Admin | Đổi vai trò |

### Tin đăng — `/api/properties`, `/api/favorites`, `/api/uploads`

| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/properties` | Public | Tìm kiếm, lọc, sắp xếp, phân trang |
| GET | `/api/properties/stats` | Public | Số tin đang cho thuê theo loại nhà / quận (trang chủ) |
| GET | `/api/properties/feed` | Public | Bản tin chung, sắp theo thời điểm được duyệt |
| GET | `/api/properties/{id}` | Public | Chi tiết nhà (tăng lượt xem). Email / SĐT chủ nhà chỉ trả về khi đã đăng nhập |
| GET | `/api/properties/{id}/similar` | Public | Nhà tương tự |
| GET | `/api/properties/mine` | Landlord | Nhà của tôi |
| GET | `/api/properties/manage` | Admin | Quản lý tất cả tin |
| POST | `/api/properties` | Landlord | Đăng tin |
| PUT | `/api/properties/{id}` | Landlord / Admin | Sửa tin |
| DELETE | `/api/properties/{id}` | Landlord / Admin | Xóa tin |
| PATCH | `/api/properties/{id}/status` | Landlord / Admin | Duyệt / từ chối (kèm `reason`) / ẩn / hiện |
| POST | `/api/properties/{id}/images` | Landlord / Admin | Thêm ảnh |
| DELETE | `/api/properties/{id}/images/{imageId}` | Landlord / Admin | Xóa ảnh |
| PATCH | `/api/properties/{id}/images/{imageId}/thumbnail` | Landlord / Admin | Đặt ảnh đại diện |
| POST | `/api/uploads/images` | Đăng nhập | Upload ảnh (multipart, ≤ 5MB, 40 ảnh / phút), trả URL tương đối `/uploads/...` |
| GET | `/api/uploads/images` | Đăng nhập | Ảnh tôi đã upload (Admin: tất cả) |
| GET | `/api/uploads/images/{id}` | Người upload / Admin | Thông tin một ảnh |
| DELETE | `/api/uploads/images/{id}` | Người upload / Admin | Xóa ảnh (chặn nếu đang được dùng) |
| GET | `/api/favorites` | Đăng nhập | Nhà đã lưu |
| POST | `/api/favorites/{propertyId}` | Đăng nhập | Lưu nhà |
| DELETE | `/api/favorites/{propertyId}` | Đăng nhập | Bỏ lưu |

Tham số tìm kiếm nhà: `keyword, city, district, ward, type, minPrice, maxPrice, minArea, maxArea, minBedrooms, minBathrooms, amenities (lặp lại được), latitude, longitude, radiusKm, sortBy (newest | price_asc | price_desc | area_desc | rating_desc | most_viewed | distance), page, pageSize`.

### Lịch hẹn xem nhà — `/api/appointments`

| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/appointments` | Tenant | Đặt lịch xem nhà |
| GET | `/api/appointments/{id}` | Liên quan | Chi tiết |
| GET | `/api/appointments/mine` | Tenant | Lịch hẹn của tôi |
| GET | `/api/appointments/received` | Landlord | Lịch hẹn xem nhà của tôi |
| PATCH | `/api/appointments/{id}/confirm` | Landlord / Admin | Xác nhận |
| PATCH | `/api/appointments/{id}/reject` | Landlord / Admin | Từ chối |
| PATCH | `/api/appointments/{id}/cancel` | Liên quan | Hủy |
| PATCH | `/api/appointments/{id}/complete` | Landlord / Admin | Đã dẫn khách xem |

### Thuê nhà, hóa đơn, thanh toán

| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| POST | `/api/bookings` | Tenant | Gửi yêu cầu thuê |
| GET | `/api/bookings` | Admin | Tất cả yêu cầu thuê / hợp đồng |
| GET | `/api/bookings/{id}` | Liên quan | Chi tiết |
| GET | `/api/bookings/mine` | Tenant | Yêu cầu của tôi |
| GET | `/api/bookings/received` | Landlord | Yêu cầu nhận được |
| PATCH | `/api/bookings/{id}/approve` | Landlord / Admin | Duyệt |
| PATCH | `/api/bookings/{id}/reject` | Landlord / Admin | Từ chối |
| PATCH | `/api/bookings/{id}/cancel` | Tenant / Admin | Hủy |
| PATCH | `/api/bookings/{id}/complete` | Landlord / Admin | Kết thúc hợp đồng |
| POST | `/api/bookings/{id}/invoices` | Landlord / Admin | Lập hóa đơn tháng |
| GET | `/api/bookings/{id}/invoices` | Liên quan | Hóa đơn của hợp đồng |
| GET | `/api/invoices/{id}` | Liên quan | Chi tiết hóa đơn |
| GET | `/api/invoices/mine` | Tenant | Hóa đơn của tôi |
| GET | `/api/invoices/received` | Landlord | Hóa đơn đã lập |
| PATCH | `/api/invoices/{id}/cancel` | Landlord / Admin | Hủy hóa đơn chưa thanh toán |
| POST | `/api/bookings/{id}/payments` | Tenant | Tạo thanh toán (có thể kèm `invoiceId`) |
| GET | `/api/bookings/{id}/payments` | Liên quan | Lịch sử thanh toán của hợp đồng |
| GET | `/api/payments/mine` | Tenant | Thanh toán của tôi |
| GET | `/api/payments/received` | Landlord | Thanh toán nhận được |
| PATCH | `/api/payments/{id}/status` | Landlord / Admin | Xác nhận Success / Failed, hoặc Refunded |

### Đánh giá, tin nhắn, thông báo, báo cáo, thống kê

| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| GET | `/api/properties/{id}/reviews` | Public | Đánh giá của nhà |
| GET | `/api/properties/{id}/reviews/eligibility` | Tenant | Tôi có được đánh giá căn này không (đã thuê, chưa đánh giá) |
| POST | `/api/properties/{id}/reviews` | Tenant | Viết đánh giá |
| PUT | `/api/reviews/{id}` | Tenant (người viết) | Sửa đánh giá |
| PUT | `/api/reviews/{id}/reply` | Landlord / Admin | Phản hồi đánh giá |
| DELETE | `/api/reviews/{id}` | Người viết / Admin | Xóa đánh giá |
| POST | `/api/conversations` | Đăng nhập | Bắt đầu trò chuyện |
| POST | `/api/conversations/support` | Tenant / Landlord | Nhắn tin cho quản trị viên (hỗ trợ) |
| GET | `/api/conversations` | Đăng nhập | Danh sách trò chuyện |
| GET | `/api/conversations/unread-count` | Đăng nhập | Số tin chưa đọc |
| GET | `/api/conversations/{id}/messages` | Người tham gia | Tin nhắn (tự đánh dấu đã đọc) |
| POST | `/api/conversations/{id}/messages` | Người tham gia | Gửi tin nhắn |
| GET | `/api/notifications` | Đăng nhập | Thông báo của tôi |
| GET | `/api/notifications/unread-count` | Đăng nhập | Số thông báo chưa đọc |
| PATCH | `/api/notifications/{id}/read` | Đăng nhập | Đánh dấu đã đọc |
| PATCH | `/api/notifications/read-all` | Đăng nhập | Đánh dấu tất cả đã đọc |
| DELETE | `/api/notifications/{id}` | Đăng nhập | Xóa thông báo |
| POST | `/api/properties/{id}/reports` | Đăng nhập | Báo cáo tin vi phạm |
| GET | `/api/reports/mine` | Đăng nhập | Báo cáo tôi đã gửi |
| GET | `/api/reports` | Admin | Danh sách báo cáo |
| PATCH | `/api/reports/{id}` | Admin | Xử lý báo cáo |
| GET | `/api/statistics/admin` | Admin | Thống kê hệ thống |
| GET | `/api/statistics/landlord` | Landlord | Thống kê chủ nhà |
| GET | `/api/statistics/tenant` | Tenant | Thống kê người thuê |

### Định dạng response

```json
{
  "success": true,
  "message": "Đăng nhập thành công",
  "data": { ... }
}
```

Lỗi (validate, nghiệp vụ, 401, 403, 500) đều trả cùng định dạng với `success: false` và `errors`.

## Chạy dự án

Dự án gồm hai phần chạy riêng:

| Phần | Thư mục | Công nghệ | Địa chỉ khi chạy |
|---|---|---|---|
| Backend (API) | thư mục gốc | ASP.NET Core 10 + SQL Server | http://localhost:5224 |
| Frontend (Web) | `frontend/` | React 18 + TypeScript + Vite | http://localhost:5173 |

Frontend không gọi thẳng tới cổng 5224. Trình duyệt gọi `/api` và `/uploads` trên chính cổng 5173, rồi Vite chuyển tiếp các request đó sang backend (proxy). Vì vậy **phải chạy backend thì web mới có dữ liệu**.

### 1. Cài đặt công cụ

| Công cụ | Phiên bản | Kiểm tra bằng lệnh | Ghi chú |
|---|---|---|---|
| .NET SDK | 10.x | `dotnet --list-sdks` | https://dotnet.microsoft.com/download/dotnet/10.0 |
| Node.js | 18 trở lên (khuyên dùng bản LTS) | `node -v` và `npm -v` | https://nodejs.org |
| SQL Server LocalDB | bất kỳ | `sqllocaldb info` (phải thấy `MSSQLLocalDB`) | Có sẵn khi cài Visual Studio với workload *ASP.NET and web development* hoặc *Data storage and processing*. Nếu không dùng Visual Studio thì cài **SQL Server Express** và chọn mục **LocalDB**. |

Nên cài thêm **SQL Server Management Studio (SSMS)** hoặc dùng *SQL Server Object Explorer* trong Visual Studio để xem dữ liệu. Không cài cũng chạy được.

> Dùng SQL Server thật thay cho LocalDB: sửa `ConnectionStrings:DefaultConnection` trong `appsettings.json`, ví dụ
> `Server=localhost;Database=HouseRentalAppDb;User Id=sa;Password=MatKhauCuaBan;TrustServerCertificate=True`

### 2. Chuẩn bị mã nguồn

- Mở terminal (PowerShell, CMD hoặc Git Bash) tại **thư mục gốc của dự án**, tức thư mục chứa `HouseRentalAPI.csproj`.
- **Ảnh của tin đăng mẫu** nằm trong `wwwroot/uploads/nhatot/` và bị `.gitignore` loại khỏi Git. Nếu lấy mã nguồn qua Git, cần chép riêng thư mục này vào đúng đường dẫn `wwwroot/uploads/nhatot/`. Thiếu thư mục này thì web vẫn chạy, chỉ không hiện được ảnh các tin đăng mẫu.

### 3. Chạy backend

Tại thư mục gốc:

```bash
dotnet restore
dotnet run --launch-profile http
```

Backend đã sẵn sàng khi terminal in ra dòng:

```
Now listening on: http://localhost:5224
```

- **Không cần tự tạo database.** Ở môi trường Development, lần chạy đầu tiên sẽ tự áp dụng migration, tạo database `HouseRentalAppDb` trên LocalDB và nạp dữ liệu mẫu (tài khoản, tin đăng ở Đà Nẵng, hợp đồng, hóa đơn, tin nhắn...). Lần đầu có thể mất từ vài chục giây đến 1–2 phút. Những lần sau chạy nhanh hơn vì dữ liệu đã có.
- Phải chạy profile **`http`**. Profile `https` bật chuyển hướng sang https://localhost:7004, khiến proxy của frontend không gọi được backend.
- Kiểm tra nhanh: mở http://localhost:5224/swagger hoặc http://localhost:5224/health. Nếu thấy `"status": "Healthy"` thì đã kết nối được database.
- Dùng Visual Studio: mở `HouseRentalAPI.sln`, chọn profile **http** trên thanh công cụ rồi bấm Run (F5).
- Dừng backend: bấm `Ctrl + C` trong terminal.

### 4. Chạy frontend

Mở **một terminal khác**, để terminal backend tiếp tục chạy:

```bash
cd frontend
npm install        # chỉ cần chạy lần đầu, hoặc khi package.json thay đổi
npm run dev
```

Frontend đã sẵn sàng khi terminal in ra `Local: http://localhost:5173/`. Mở địa chỉ này bằng trình duyệt.

- File `frontend/.env` khai báo địa chỉ backend cho proxy: `BACKEND_URL=http://localhost:5224`. Nếu chưa có file này thì sao chép từ `frontend/.env.example`. Thiếu file thì frontend vẫn dùng mặc định `http://localhost:5224`.
- Nếu web báo *"Không kết nối được tới backend"* thì backend chưa chạy hoặc chưa khởi động xong.
- Build bản production: `npm run build`, kết quả nằm trong `frontend/dist/`.

### 5. Chạy cả hai bằng một lệnh (tùy chọn)

Thay cho bước 3 và 4, có thể chạy backend và frontend chung trong một terminal:

```bash
cd frontend
npm install
npm run dev:all    # API http://localhost:5224 + Web http://localhost:5173
```

Lệnh `dev:all` dùng gói `concurrently` để chạy đồng thời `dotnet run --project .. --launch-profile http` và `vite`. Log của hai bên được đánh dấu `[api]` và `[web]`. Bấm `Ctrl + C` sẽ dừng cả hai.

### 6. Các địa chỉ sau khi chạy

| Địa chỉ | Nội dung |
|---|---|
| http://localhost:5173 | Giao diện web |
| http://localhost:5224/swagger | Swagger UI: thử API, bấm **Authorize** rồi dán access token |
| http://localhost:5224/scalar | Tài liệu API dạng Scalar |
| http://localhost:5224/openapi/v1.json | Tài liệu OpenAPI (JSON) |
| http://localhost:5224/health | Trạng thái API và database |

File `HouseRentalAPI.http` có sẵn request mẫu, mở bằng Visual Studio hoặc extension REST Client của VS Code.

### 7. Lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| `dotnet` báo không tìm thấy SDK hoặc yêu cầu `net10.0` | Cài .NET SDK 10, mở lại terminal rồi kiểm tra bằng `dotnet --list-sdks`. |
| Backend báo lỗi kết nối SQL Server, `A network-related or instance-specific error...` | Chưa có LocalDB hoặc LocalDB chưa chạy. Kiểm tra bằng `sqllocaldb info`; khởi động bằng `sqllocaldb start MSSQLLocalDB`. Nếu dùng SQL Server khác thì sửa connection string trong `appsettings.json`. |
| `Address already in use` / `Port 5173 is in use` | Có chương trình khác (thường là lần chạy trước chưa tắt) đang giữ cổng. Tìm PID bằng `netstat -ano \| findstr :5224` (hoặc `:5173`), rồi tắt bằng `taskkill /PID <PID> /F`. |
| `npm` báo lỗi khi `npm install` | Kiểm tra Node.js ≥ 18. Xóa thư mục `frontend/node_modules` rồi chạy lại `npm install`. |
| Web mở được nhưng không có dữ liệu, hoặc báo lỗi 503 | Backend chưa chạy, chưa khởi động xong, hoặc đang chạy profile `https`. Chạy lại bằng `dotnet run --launch-profile http`. |
| Tin đăng không hiện ảnh | Thiếu thư mục `wwwroot/uploads/nhatot/` (xem mục 2). |
| Muốn xóa database để tạo lại dữ liệu mẫu từ đầu | Tắt backend, chạy `dotnet tool restore` rồi `dotnet ef database drop -f`, sau đó chạy lại backend. |

### Tài khoản mẫu

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | admin@houserental.vn | Admin@123 |
| Landlord | landlord@houserental.vn | Landlord@123 |
| Tenant | tenant@houserental.vn | Tenant@123 |
| Tenant | tenant2@houserental.vn | Tenant@123 |
| Landlord | landlord2@houserental.vn | Landlord@123 |

`DemoSeeder` (chạy một lần khi khởi động) bổ sung dữ liệu để trình diễn:

- **Chat với quản trị viên**: tenant, tenant2 và landlord đều có sẵn cuộc trò chuyện hỗ trợ với admin.
  Người thuê / chủ nhà bấm "Nhắn tin cho quản trị viên" ở trang Tin nhắn (`POST /api/conversations/support`).
- **5 tin chờ duyệt**: admin vào "Duyệt tin đăng" → bấm Duyệt thì tin hiển thị công khai (tìm kiếm, trang chủ, chi tiết)
  và chủ nhà nhận thông báo; bấm Từ chối kèm lý do thì chủ nhà sửa tin để gửi duyệt lại.
- **9 hợp đồng** đủ trạng thái: chờ duyệt (2 người cùng xin thuê căn hộ Sơn Trà — duyệt 1 người thì người kia tự bị từ chối),
  đang hiệu lực (có hóa đơn đã trả, hóa đơn trả một phần, thanh toán chờ chủ nhà xác nhận), đã kết thúc, bị từ chối, đã hủy.

### Bản tin & đăng tin

- **Bản tin** (`/ban-tin`, nút "Bản tin" trên thanh menu): mọi tin đã được duyệt dạng bài viết; tin vừa được admin duyệt nằm đầu.
- **Bản tin cá nhân**: trang cá nhân của chủ nhà (`/users/{id}`, menu tài khoản → "Trang cá nhân & bản tin") có tab Bản tin;
  chính chủ thấy cả tin chờ duyệt / bị từ chối.
- **Đăng tin** (`/dang-tin`): form 5 bước, ghim vị trí trên bản đồ, xem trước, tự lưu nháp.

### Bản đồ nhà cho thuê gần bạn (trang chủ)

- Bấm **"Dùng vị trí của tôi"** (trình duyệt hỏi quyền truy cập vị trí) hoặc **nhập địa chỉ** ở Đà Nẵng.
- Địa chỉ được đổi sang toạ độ bằng OpenStreetMap Nominatim; frontend gọi
  `GET /api/properties?latitude=&longitude=&radiusKm=&sortBy=distance` và đánh dấu các nhà trên bản đồ (Leaflet + OpenStreetMap).
- Chọn bán kính 1–10 km; các tin trùng vị trí được gộp thành một điểm; mỗi tin có nút **chỉ đường trên Google Maps**.
- Không dùng Google Maps JavaScript API vì API này bắt buộc có API key gắn tài khoản thanh toán Google Cloud.

### Kiểm tra sức khỏe hệ thống

`GET /health` trả trạng thái API và kết nối cơ sở dữ liệu (HTTP 200 khi `Healthy`, 503 khi lỗi):

```json
{ "status": "Healthy", "totalDurationMs": 55, "checks": [{ "name": "database", "status": "Healthy" }] }
```

### Unit test

Project `tests/HouseRentalAPI.Tests` (xUnit + Moq) kiểm thử các luật nghiệp vụ chính:
tạo / duyệt / hủy yêu cầu thuê, xác nhận thanh toán và cộng dồn vào hóa đơn, trạng thái hóa đơn, cấp và kiểm tra JWT.

```bash
dotnet test HouseRentalAPI.sln
```

### Dữ liệu test cho phân trang (500 người dùng)

Script SQL `Data/Scripts/seed-test-users.sql` tạo `user1@example.com` … `user500@example.com`
(mật khẩu `Password123`, băm BCrypt nên đăng nhập được), vai trò chia theo `% 15` → Admin, `% 5` → Landlord, còn lại Tenant;
một số tài khoản bị khóa để thử lọc `IsActive`. Chạy lại không bị trùng.

```bash
sqlcmd -S "(localdb)\mssqllocaldb" -d HouseRentalAppDb -f 65001 -i Data\Scripts\seed-test-users.sql
# Xóa dữ liệu test:
sqlcmd -S "(localdb)\mssqllocaldb" -d HouseRentalAppDb -f 65001 -i Data\Scripts\cleanup-test-users.sql
```

Thử trên Swagger: `GET /api/users?Page=1&PageSize=10`, `Keyword=Nguyễn Văn`, `Role=Admin&IsActive=false`, `SortBy=name_asc`.

### EF Migrations

`dotnet-ef` được khai báo là local tool trong `dotnet-tools.json`:

```bash
dotnet tool restore
dotnet ef migrations add <TenMigration> -o Data/Migrations
dotnet ef database update
```

## Bảo mật

- Mật khẩu băm bằng BCrypt; refresh token và mã đặt lại mật khẩu chỉ lưu bản băm SHA-256.
- Refresh token xoay vòng (dùng một lần), đổi mật khẩu / khóa tài khoản / đổi vai trò thì thu hồi mọi phiên.
- Access token sống 30 phút (frontend tự refresh). Mỗi request, `JwtBearerEvents.OnTokenValidated` đối chiếu
  `IsActive` và `Role` hiện tại trong CSDL: Admin **khóa tài khoản hoặc đổi vai trò thì token cũ bị từ chối (401) ngay
  ở request kế tiếp**, không phải chờ token hết hạn.
- Giới hạn tần suất (rate limiting): API đăng nhập, đăng ký, làm mới token, quên / đặt lại mật khẩu tối đa
  10 lần / phút / IP; upload ảnh tối đa 40 lần / phút / người dùng (request ≤ 15MB); toàn bộ API tối đa 300 request / phút / IP. Vượt giới hạn trả `429` kèm header `Retry-After`.
- Header bảo mật: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`.
- Upload ảnh kiểm tra chữ ký file (magic bytes), không tin phần mở rộng do client gửi; chỉ người upload / Admin
  xem được thông tin ảnh theo id (chống IDOR).
- API công khai không lộ email / SĐT chủ nhà khi chưa đăng nhập.
- `ErrorsController` (giả lập lỗi để demo middleware) chỉ hoạt động ở Development, môi trường khác trả 404.

## Ràng buộc nghiệp vụ quan trọng

- **Hóa đơn**: unique index `(BookingId, Year, Month)` có filter `[Status] <> 'Cancelled'` → hủy hóa đơn lập nhầm rồi lập lại tháng đó được.
- **Duyệt hợp đồng** chỉ khi tin đang `Available` và chưa quá ngày kết thúc → tin bị Admin ẩn không thể "lách" bằng Approve → Complete.
- **Trạng thái nhà**: nhà đang có hợp đồng `Approved` không thể chuyển về `Available` / `PendingApproval` / `Rejected`;
  chỉ nhà có hợp đồng hiệu lực mới được ở `Rented`.
- **Tiền cọc**: mỗi hợp đồng chỉ một phiếu cọc đang chờ hoặc đã thành công, số tiền không vượt mức cọc; chủ nhà không thể xác nhận phiếu cọc thứ hai.
- **Giờ Việt Nam**: "hôm nay" (ngày bắt đầu thuê, hạn hóa đơn, hết hạn hợp đồng) và giờ trong thông báo lịch hẹn tính theo UTC+7 (`Constants/AppTime.cs`); CSDL vẫn lưu UTC.

## Ghi chú khi lên production

- Đổi `Jwt:Key` thành chuỗi ngẫu nhiên dài, lưu bằng biến môi trường / User Secrets, không commit.
- Đặt `Database:SeedOnStartup = false`.
- Cấu hình `Cors:AllowedOrigins` đúng domain frontend.
- Thay `LoggingEmailSender` bằng dịch vụ gửi email thật (SMTP, SendGrid...).
- Thay `LocalFileStorageService` bằng S3 / Azure Blob / Cloudinary nếu chạy nhiều server.
