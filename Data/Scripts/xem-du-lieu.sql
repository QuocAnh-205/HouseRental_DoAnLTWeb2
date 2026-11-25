/* =====================================================================
   XEM DỮ LIỆU DỰ ÁN HOUSE RENTAL TRONG SSMS
   Server  : (localdb)\mssqllocaldb      (Authentication: Windows Authentication)
   Database: HouseRentalAppDb
   Bôi đen từng đoạn rồi bấm F5 (Execute) để chạy riêng, hoặc F5 cả file để chạy hết.
   ===================================================================== */

USE HouseRentalAppDb;
GO

-- 1. Tổng quan: số dòng của từng bảng
SELECT t.name AS [Bảng], SUM(p.rows) AS [Số dòng]
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
WHERE t.name <> '__EFMigrationsHistory'
GROUP BY t.name
ORDER BY [Số dòng] DESC;

-- 2. Người dùng: 20 người mới nhất (tài khoản mẫu, chủ nhà Nhà Tốt, 500 tài khoản test)
SELECT TOP 20 Id, FullName, Email, PhoneNumber, Role, IsActive, LastLoginAt, CreatedAt
FROM Users
ORDER BY Id DESC;

-- 3. Số người dùng theo vai trò
SELECT Role, COUNT(*) AS [Số lượng], SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS [Bị khóa]
FROM Users
GROUP BY Role;

-- 4. Tin đăng cho thuê: 20 tin mới nhất kèm tên chủ nhà
SELECT TOP 20 p.Id, p.Title, p.Type, p.PricePerMonth, p.Area, p.District, p.Status,
       u.FullName AS ChuNha, p.ViewCount, p.CreatedAt, p.PublishedAt
FROM Properties p
JOIN Users u ON u.Id = p.LandlordId
ORDER BY p.Id DESC;

-- 5. Tin đăng đang chờ admin duyệt
SELECT p.Id, p.Title, p.PricePerMonth, u.FullName AS ChuNha, p.CreatedAt
FROM Properties p
JOIN Users u ON u.Id = p.LandlordId
WHERE p.Status = 'PendingApproval';

-- 6. Hợp đồng thuê (Bookings) kèm tên nhà và người thuê
SELECT b.Id, p.Title AS Nha, t.FullName AS NguoiThue, b.StartDate, b.EndDate,
       b.MonthlyPrice, b.Deposit, b.Status, b.RejectReason
FROM Bookings b
JOIN Properties p ON p.Id = b.PropertyId
JOIN Users t ON t.Id = b.TenantId
ORDER BY b.Id;

-- 7. Hóa đơn hàng tháng
SELECT i.Id, i.BookingId, CONCAT(i.Month, '/', i.Year) AS Ky, i.RentAmount, i.ElectricityAmount,
       i.WaterAmount, i.TotalAmount, i.PaidAmount, i.DueDate, i.Status
FROM Invoices i
ORDER BY i.Id;

-- 8. Thanh toán
SELECT pay.Id, pay.BookingId, pay.InvoiceId, pay.Amount, pay.Type, pay.Method, pay.Status,
       pay.TransactionCode, pay.PaidAt
FROM Payments pay
ORDER BY pay.Id;

-- 9. Tin nhắn: các cuộc trò chuyện và tin nhắn mới nhất
SELECT TOP 30 m.Id, m.ConversationId, s.FullName AS NguoiGui, m.Content, m.IsRead, m.CreatedAt
FROM Messages m
JOIN Users s ON s.Id = m.SenderId
ORDER BY m.Id DESC;

-- 10. Thông báo mới nhất
SELECT TOP 30 n.Id, u.FullName AS NguoiNhan, n.Type, n.Title, n.Link, n.IsRead, n.CreatedAt
FROM Notifications n
JOIN Users u ON u.Id = n.UserId
ORDER BY n.Id DESC;

-- 11. Lịch sử migration (EF Core đã tạo bảng nào, lúc nào)
SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;
