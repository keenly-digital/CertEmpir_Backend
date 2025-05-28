using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CertEmpire.Migrations
{
    /// <inheritdoc />
    public partial class DomainModelAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Domains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DomainId = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainName = table.Column<string>(type: "text", nullable: false),
                    IncludeQuestions = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeAnswers = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeExplanations = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeComments = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domains", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Domains");
        }
    }
}
