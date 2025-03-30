using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddRepositoryMappingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RepositoryMappings",
                columns: table => new
                {
                    MappingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevCommunityRepositoryId = table.Column<int>(type: "int", nullable: false),
                    GiteaRepositoryId = table.Column<int>(type: "int", nullable: false),
                    HtmlUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CloneUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SshUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryMappings", x => x.MappingId);
                    table.ForeignKey(
                        name: "FK_RepositoryMappings_Repositories",
                        column: x => x.DevCommunityRepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "RepositoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryMappings_DevCommunityRepositoryId",
                table: "RepositoryMappings",
                column: "DevCommunityRepositoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepositoryMappings");
        }
    }
}
