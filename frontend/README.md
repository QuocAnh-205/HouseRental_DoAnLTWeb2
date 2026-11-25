# TổẤm — Frontend cho HouseRentalAPI

Web frontend React 18 + TypeScript + Vite cho backend `HouseRentalAPI` (thư mục cha). Không dùng thư viện UI; CSS viết tay, responsive.

## Chạy

```bash
cd frontend
npm install
npm run dev:all        # chạy cả backend (http://localhost:5224) và frontend (http://localhost:5173)
```

Hoặc chạy riêng: `npm run backend` và `npm run dev` ở hai terminal.

Frontend gọi API cùng origin (`/api`, `/uploads`), Vite proxy chuyển tiếp sang backend nên không phụ thuộc CORS hay cổng dev server. Địa chỉ backend đặt ở `BACKEND_URL` trong `.env` (mặc định `http://localhost:5224`). Nếu web báo *"Không kết nối được tới backend"* thì backend chưa chạy.

> Backend phải chạy profile **http**. Profile https bật `UseHttpsRedirection`, request vào 5224 sẽ bị chuyển hướng sang 7004.

Icon dùng [Font Awesome](https://fontawesome.com) (`@fortawesome/react-fontawesome`, bộ solid + regular miễn phí).

Tài khoản mẫu (có nút điền nhanh ở trang đăng nhập): `admin@houserental.vn / Admin@123`, `landlord@houserental.vn / Landlord@123`, `tenant@houserental.vn / Tenant@123`.

## Chức năng theo vai trò

| Vai trò | Trang |
|---|---|
| Khách | Tìm kiếm & lọc nhà (từ khóa, thành phố, quận, loại, giá, diện tích, phòng, tiện ích, quanh vị trí), chi tiết nhà (ảnh, bản đồ, đánh giá, nhà tương tự), hồ sơ chủ nhà, đăng ký / đăng nhập / quên mật khẩu |
| Người thuê | Lưu nhà, gửi yêu cầu thuê, đặt lịch xem, nhắn tin chủ nhà, báo cáo tin, đánh giá; hợp đồng → hóa đơn → thanh toán; dashboard |
| Chủ nhà | Đăng / sửa / ẩn / xóa tin, upload & quản lý ảnh; duyệt / từ chối / kết thúc hợp đồng; lập hóa đơn điện nước; xác nhận / hoàn tiền thanh toán; xác nhận lịch hẹn; phản hồi đánh giá; dashboard doanh thu 12 tháng |
| Admin | Dashboard hệ thống, duyệt / từ chối / ẩn tin, quản lý người dùng (khóa, đổi vai trò), xem mọi hợp đồng, xử lý báo cáo vi phạm |
| Chung | Thông báo (tự đếm chưa đọc), tin nhắn (tự làm mới), hồ sơ, đổi mật khẩu, đăng xuất mọi thiết bị |

## Cấu trúc

```
src/
├── api/          client.ts (fetch + tự refresh token), types.ts (DTO), labels.ts (nhãn tiếng Việt, định dạng)
├── auth/         AuthContext (đăng nhập, lưu token trong localStorage)
├── components/   Layout, PropertyCard, RevenueChart, ui.tsx (Modal, Badge, Pagination, Tabs...)
├── pages/        Home, PropertyDetail, Bookings, Finance (hóa đơn/thanh toán/lịch hẹn), Social (tin nhắn/thông báo/...),
│                 Dashboard, auth/, landlord/, admin/
├── hooks.ts      useApi, useAction, toast
└── App.tsx       Định tuyến
```

Build production: `npm run build` → `dist/`.
