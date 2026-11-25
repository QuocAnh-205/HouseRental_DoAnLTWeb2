-- =============================================
-- Kiểm tra dữ liệu HouseRentalAppDb (chạy trong SSMS)
-- Bôi đen từng khối rồi bấm F5 để chạy riêng
-- =============================================
USE HouseRentalAppDb;
GO

-- 1. Đếm số dòng của các bảng chính
SELECT 'Users' AS Bang, COUNT(*) AS SoDong FROM Users
UNION ALL SELECT 'Properties', COUNT(*) FROM Properties
UNION ALL SELECT 'PropertyImages', COUNT(*) FROM PropertyImages
UNION ALL SELECT 'UploadedImages', COUNT(*) FROM UploadedImages
UNION ALL SELECT 'Bookings', COUNT(*) FROM Bookings
UNION ALL SELECT 'Invoices', COUNT(*) FROM Invoices
UNION ALL SELECT 'Appointments', COUNT(*) FROM Appointments
UNION ALL SELECT 'Reviews', COUNT(*) FROM Reviews;

-- 2. Tài khoản mẫu để đăng nhập (mật khẩu được băm BCrypt, không đọc được)
SELECT Id, FullName, Email, Role, IsActive, LastLoginAt
FROM Users
WHERE Email LIKE '%@houserental.vn' AND Email NOT LIKE 'nhatot%'
ORDER BY Role;

-- 3. Số người dùng theo vai trò
SELECT Role, COUNT(*) AS SoNguoi
FROM Users
GROUP BY Role;

-- 4. 10 tin đăng mới nhất kèm tên chủ nhà
SELECT TOP 10 p.Id, p.Title, p.District, p.Type, p.PricePerMonth, p.Status, u.FullName AS ChuNha
FROM Properties p
JOIN Users u ON u.Id = p.LandlordId
ORDER BY p.CreatedAt DESC;

-- 5. Số tin theo quận và giá trung bình
SELECT District, COUNT(*) AS SoTin, FORMAT(AVG(PricePerMonth), 'N0') AS GiaTrungBinh
FROM Properties
GROUP BY District
ORDER BY SoTin DESC;

-- 6. Tin đang chờ admin duyệt
SELECT Id, Title, LandlordId, CreatedAt
FROM Properties
WHERE Status = 'PendingApproval';

-- 7. Ảnh của một căn nhà (đổi số 1 thành Id nhà cần xem)
SELECT Id, Url, IsThumbnail
FROM PropertyImages
WHERE PropertyId = 1;

-- 8. Ảnh đã upload qua API /api/uploads/images (có id)
SELECT ui.Id, ui.OriginalFileName, ui.Size, ui.Url, u.Email AS NguoiUpload, ui.CreatedAt
FROM UploadedImages ui
JOIN Users u ON u.Id = ui.UploadedById
ORDER BY ui.Id DESC;

-- 9. Hợp đồng thuê kèm tên nhà và người thuê
SELECT TOP 10 b.Id, p.Title AS Nha, t.FullName AS NguoiThue, b.StartDate, b.EndDate, b.MonthlyPrice, b.Status
FROM Bookings b
JOIN Properties p ON p.Id = b.PropertyId
JOIN Users t ON t.Id = b.TenantId
ORDER BY b.CreatedAt DESC;

-- 10. Hóa đơn chưa thanh toán đủ
SELECT i.Id, i.BookingId, i.Month, i.Year, i.TotalAmount, i.PaidAmount,
       i.TotalAmount - i.PaidAmount AS ConNo, i.Status
FROM Invoices i
WHERE i.PaidAmount < i.TotalAmount;

-- 11. Các migration đã chạy (lịch sử thay đổi cấu trúc database)
SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory;
