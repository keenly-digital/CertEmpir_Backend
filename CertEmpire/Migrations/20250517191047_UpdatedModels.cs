using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertEmpire.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Questions_QuestionId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_UploadedFiles_fileId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_ReviewTasks_Reports_ReportId",
                table: "ReviewTasks");

            migrationBuilder.DropIndex(
                name: "IX_ReviewTasks_ReportId",
                table: "ReviewTasks");

            migrationBuilder.DropIndex(
                name: "IX_Reports_fileId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_QuestionId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "Reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "QuestionId",
                table: "Reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ReviewTasks_ReportId",
                table: "ReviewTasks",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_fileId",
                table: "Reports",
                column: "fileId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_QuestionId",
                table: "Reports",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Questions_QuestionId",
                table: "Reports",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "QuestionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_UploadedFiles_fileId",
                table: "Reports",
                column: "fileId",
                principalTable: "UploadedFiles",
                principalColumn: "FileId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewTasks_Reports_ReportId",
                table: "ReviewTasks",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "ReportId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
