using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseRentalAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceUniqueIgnoreCancelled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_BookingId_Year_Month",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BookingId_Year_Month",
                table: "Invoices",
                columns: new[] { "BookingId", "Year", "Month" },
                unique: true,
                filter: "[Status] <> 'Cancelled'");

            // Ảnh upload trước đây lưu URL tuyệt đối "http://localhost:5224/uploads/..." → đổi về "/uploads/..."
            // để đi qua Vite proxy / cùng domain khi deploy, không phụ thuộc cổng backend.
            foreach (var (table, column) in new[] { ("UploadedImages", "Url"), ("PropertyImages", "Url"), ("Users", "AvatarUrl") })
            {
                migrationBuilder.Sql($@"
UPDATE [{table}]
SET [{column}] = SUBSTRING([{column}], CHARINDEX('/uploads/', [{column}]), LEN([{column}]))
WHERE [{column}] LIKE 'http://localhost:%/uploads/%' OR [{column}] LIKE 'https://localhost:%/uploads/%';");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_BookingId_Year_Month",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BookingId_Year_Month",
                table: "Invoices",
                columns: new[] { "BookingId", "Year", "Month" },
                unique: true);
        }
    }
}
