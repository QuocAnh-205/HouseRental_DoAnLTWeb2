/* =====================================================================
   SEED NGƯỜI DÙNG TEST ĐỂ THỬ PHÂN TRANG / TÌM KIẾM / LỌC / SẮP XẾP
   ---------------------------------------------------------------------
   Dựa trên script mẫu (vòng lặp WHILE + chia Role theo % 15 và % 5),
   đã chỉnh cho đúng cấu trúc bảng [Users] của dự án HouseRentalAPI:

     Script mẫu                      ->  Dự án này
     -----------------------------------------------------------------
     [Username]                      ->  [FullName]  (họ tên tiếng Việt)
     [PasswordHash] SHA2_256 + Salt  ->  [PasswordHash] BCrypt (không có cột Salt)
     Role Admin / Manager / User     ->  Admin / Landlord / Tenant
     (không có)                      ->  [PhoneNumber], [IsActive], [LastLoginAt]

   - Mọi tài khoản test đăng nhập được thật: email userN@example.com, mật khẩu Password123
   - Chạy lại nhiều lần không bị trùng (kiểm tra NOT EXISTS theo Email)
   - Xóa dữ liệu test: chạy file cleanup-test-users.sql

   Cách chạy:
     sqlcmd -S "(localdb)\mssqllocaldb" -d HouseRentalAppDb -f 65001 -i Data\Scripts\seed-test-users.sql
   hoặc mở file trong SSMS / Visual Studio (SQL Server Object Explorer) rồi Execute.
   ===================================================================== */

SET NOCOUNT ON;

DECLARE @SoLuong INT = 500;     -- Số tài khoản test muốn tạo (user1 ... user500)
DECLARE @i INT = 1;

-- BCrypt của chuỗi 'Password123' (tạo bằng BCrypt.Net-Next, cùng thư viện backend dùng để đăng nhập)
DECLARE @PasswordHash NVARCHAR(MAX) = N'$2a$11$STzdkcVi0nzn8arHc1jB5uyazC1Jxbg3R1fQ5M9l5OwldsbXSaUlW';

DECLARE @FullName NVARCHAR(100);
DECLARE @Email NVARCHAR(100);
DECLARE @Phone NVARCHAR(20);
DECLARE @Role NVARCHAR(20);
DECLARE @IsActive BIT;
DECLARE @CreatedAt DATETIME2;
DECLARE @Inserted INT = 0;

BEGIN TRANSACTION;

WHILE @i <= @SoLuong
BEGIN
    -- Họ tên tiếng Việt ghép từ danh sách để tìm kiếm (Keyword) có ý nghĩa, ví dụ "Nguyễn Văn An 1"
    SET @FullName = CONCAT(
        CHOOSE(@i % 10 + 1, N'Nguyễn', N'Trần', N'Lê', N'Phạm', N'Hoàng', N'Huỳnh', N'Phan', N'Võ', N'Đặng', N'Bùi'), N' ',
        CHOOSE(@i % 4 + 1, N'Văn', N'Thị', N'Minh', N'Ngọc'), N' ',
        CHOOSE(@i % 12 + 1, N'An', N'Bình', N'Chi', N'Dũng', N'Giang', N'Hà', N'Khoa', N'Lan', N'Nam', N'Phúc', N'Quân', N'Trang'),
        N' ', @i);
    SET @Email = CONCAT(N'user', @i, N'@example.com');
    SET @Phone = CONCAT(N'09', RIGHT(CONCAT(N'00000000', @i), 8));

    -- Chia vai trò giống script mẫu: Admin (chia hết 15) / Manager -> Landlord (chia hết 5) / User -> Tenant
    IF @i % 15 = 0
        SET @Role = N'Admin';
    ELSE IF @i % 5 = 0
        SET @Role = N'Landlord';
    ELSE
        SET @Role = N'Tenant';

    -- Một số tài khoản bị khóa để thử lọc IsActive
    SET @IsActive = CASE WHEN @i % 20 = 0 THEN 0 ELSE 1 END;

    -- Ngày tạo lùi dần theo @i để thử sắp xếp newest / oldest
    SET @CreatedAt = DATEADD(DAY, -@i, SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = @Email)
    BEGIN
        INSERT INTO [Users] ([FullName], [Email], [PasswordHash], [PhoneNumber], [Role], [IsActive], [LastLoginAt], [CreatedAt])
        VALUES (
            @FullName,
            @Email,
            @PasswordHash,
            @Phone,
            @Role,
            @IsActive,
            CASE WHEN @i % 3 = 0 THEN DATEADD(HOUR, -@i, SYSUTCDATETIME()) ELSE NULL END,
            @CreatedAt
        );
        SET @Inserted = @Inserted + 1;
    END

    SET @i = @i + 1;
END

COMMIT TRANSACTION;

-- Tổng kết
PRINT CONCAT(N'Đã thêm ', @Inserted, N' tài khoản test (bỏ qua ', @SoLuong - @Inserted, N' tài khoản đã có).');

SELECT N'Tổng số người dùng' AS [Thống kê], COUNT(*) AS [Số lượng] FROM [Users]
UNION ALL
SELECT CONCAT(N'Tài khoản test - ', [Role]), COUNT(*) FROM [Users] WHERE [Email] LIKE N'user%@example.com' GROUP BY [Role]
UNION ALL
SELECT N'Tài khoản test bị khóa', COUNT(*) FROM [Users] WHERE [Email] LIKE N'user%@example.com' AND [IsActive] = 0;
