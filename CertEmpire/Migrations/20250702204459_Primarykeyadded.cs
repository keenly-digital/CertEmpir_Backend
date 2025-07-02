using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertEmpire.Migrations
{
    /// <inheritdoc />
    public partial class Primarykeyadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Rewards",
                table: "Rewards");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rewards",
                table: "Rewards",
                column: "RewardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Rewards",
                table: "Rewards");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rewards",
                table: "Rewards",
                column: "Id");
        }
    }
}
