using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseRentalAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertySortIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Properties_Latitude_Longitude",
                table: "Properties",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Status_CreatedAt",
                table: "Properties",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Status_Type_ViewCount",
                table: "Properties",
                columns: new[] { "Status", "Type", "ViewCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Properties_Latitude_Longitude",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_Status_CreatedAt",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_Status_Type_ViewCount",
                table: "Properties");
        }
    }
}
