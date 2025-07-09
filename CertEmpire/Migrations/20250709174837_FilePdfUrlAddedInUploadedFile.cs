using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertEmpire.Migrations
{
    /// <inheritdoc />
    public partial class FilePdfUrlAddedInUploadedFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePdfURL",
                table: "UploadedFiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileQzsURL",
                table: "UploadedFiles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePdfURL",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "FileQzsURL",
                table: "UploadedFiles");
        }
    }
}
