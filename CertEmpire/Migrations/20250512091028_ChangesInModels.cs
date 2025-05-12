using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CertEmpire.Migrations
{
    /// <inheritdoc />
    public partial class ChangesInModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "ReviewTasks");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ReviewTasks");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "UploadedFiles",
                newName: "FileURL");

            migrationBuilder.RenameColumn(
                name: "QuestionId",
                table: "ReviewTasks",
                newName: "ReviewerUserId");

            migrationBuilder.RenameColumn(
                name: "FileId",
                table: "ReviewTasks",
                newName: "ReportId");

            migrationBuilder.RenameColumn(
                name: "TaskId",
                table: "ReviewTasks",
                newName: "ReviewTaskId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ReviewTasks",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "ReviewTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserFilePrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilePriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFilePrices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewTasks_ReportId",
                table: "ReviewTasks",
                column: "ReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewTasks_Reports_ReportId",
                table: "ReviewTasks",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "ReportId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewTasks_Reports_ReportId",
                table: "ReviewTasks");

            migrationBuilder.DropTable(
                name: "UserFilePrices");

            migrationBuilder.DropIndex(
                name: "IX_ReviewTasks_ReportId",
                table: "ReviewTasks");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ReviewTasks");

            migrationBuilder.RenameColumn(
                name: "FileURL",
                table: "UploadedFiles",
                newName: "FilePath");

            migrationBuilder.RenameColumn(
                name: "ReviewerUserId",
                table: "ReviewTasks",
                newName: "QuestionId");

            migrationBuilder.RenameColumn(
                name: "ReportId",
                table: "ReviewTasks",
                newName: "FileId");

            migrationBuilder.RenameColumn(
                name: "ReviewTaskId",
                table: "ReviewTasks",
                newName: "TaskId");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "UploadedFiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "UploadedFiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "ReviewTasks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "ReviewTasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "ReviewTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.DeviceId);
                });
        }
    }
}
