using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseRentalAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyPublishedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Properties",
                type: "datetime2",
                nullable: true);

            // Tin đã duyệt từ trước: coi thời điểm đăng là thời điểm xuất hiện trên Bản tin
            migrationBuilder.Sql(
                "UPDATE Properties SET PublishedAt = CreatedAt " +
                "WHERE PublishedAt IS NULL AND Status IN ('Available', 'Rented', 'Hidden')");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Status_PublishedAt",
                table: "Properties",
                columns: new[] { "Status", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Properties_Status_PublishedAt",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Properties");
        }
    }
}
