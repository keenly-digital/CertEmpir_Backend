using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertEmpire.Migrations
{
    /// <inheritdoc />
    public partial class OrderIdandProductIdAddedInUserFilePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FilePrice",
                table: "UserFilePrices",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "UserFilePrices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "UserFilePrices",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePrice",
                table: "UserFilePrices");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "UserFilePrices");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "UserFilePrices");
        }
    }
}
