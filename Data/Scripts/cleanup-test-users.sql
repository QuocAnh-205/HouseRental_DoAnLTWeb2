/* =====================================================================
   XÓA CÁC TÀI KHOẢN TEST do seed-test-users.sql tạo ra (email userN@example.com)
   - Chỉ xóa tài khoản test chưa phát sinh dữ liệu nghiệp vụ (tin đăng, hợp đồng, tin nhắn...)
   - Không đụng tới tài khoản mẫu (@houserental.vn) và dữ liệu Nhà Tốt

   Cách chạy:
     sqlcmd -S "(localdb)\mssqllocaldb" -d HouseRentalAppDb -f 65001 -i Data\Scripts\cleanup-test-users.sql
   ===================================================================== */

SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @Ids TABLE (Id INT PRIMARY KEY);

INSERT INTO @Ids (Id)
SELECT u.Id
FROM [Users] u
WHERE u.[Email] LIKE N'user%@example.com'
  AND NOT EXISTS (SELECT 1 FROM [Properties]    p WHERE p.LandlordId = u.Id)
  AND NOT EXISTS (SELECT 1 FROM [Bookings]      b WHERE b.TenantId = u.Id)
  AND NOT EXISTS (SELECT 1 FROM [Appointments]  a WHERE a.TenantId = u.Id)
  AND NOT EXISTS (SELECT 1 FROM [Reviews]       r WHERE r.TenantId = u.Id)
  AND NOT EXISTS (SELECT 1 FROM [Reports]       r WHERE r.ReporterId = u.Id)
  AND NOT EXISTS (SELECT 1 FROM [Conversations] c WHERE c.InitiatorId = u.Id OR c.RecipientId = u.Id);

-- Dữ liệu phụ thuộc không quan trọng của tài khoản test
DELETE FROM [Notifications] WHERE UserId IN (SELECT Id FROM @Ids);
DELETE FROM [RefreshTokens] WHERE UserId IN (SELECT Id FROM @Ids);
DELETE FROM [Favorites]     WHERE UserId IN (SELECT Id FROM @Ids);

DELETE FROM [Users] WHERE Id IN (SELECT Id FROM @Ids);
PRINT CONCAT(N'Đã xóa ', @@ROWCOUNT, N' tài khoản test.');

COMMIT TRANSACTION;

SELECT COUNT(*) AS [Tổng số người dùng còn lại] FROM [Users];
