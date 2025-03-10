using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Votes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Votes",
                type: "datetime",
                nullable: false,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true,
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<int>(
                name: "AnswerId",
                table: "Votes",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ViewCount",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Questions",
                type: "datetime",
                nullable: false,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true,
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<int>(
                name: "AnswerId",
                table: "Comments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Answers",
                columns: table => new
                {
                    AnswerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Answers__D482500454278ED7", x => x.AnswerId);
                    table.ForeignKey(
                        name: "FK__Answers__Questio__2CF2ADDF",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__Answers__UserId__2DE6D218",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionTag",
                columns: table => new
                {
                    QuestionTagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionTag", x => x.QuestionTagId);
                    table.ForeignKey(
                        name: "FK_QuestionTag_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionTag_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserIgnoredTags",
                columns: table => new
                {
                    UserIgnoredTagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserIgnoredTags", x => x.UserIgnoredTagId);
                    table.ForeignKey(
                        name: "FK_UserIgnoredTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserIgnoredTags_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserWatchedTags",
                columns: table => new
                {
                    UserWatchedTagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWatchedTags", x => x.UserWatchedTagId);
                    table.ForeignKey(
                        name: "FK_UserWatchedTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserWatchedTags_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Votes_AnswerId",
                table: "Votes",
                column: "AnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AnswerId",
                table: "Comments",
                column: "AnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_UserId",
                table: "Answers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionTag_QuestionId",
                table: "QuestionTag",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionTag_TagId",
                table: "QuestionTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_UserIgnoredTags_TagId",
                table: "UserIgnoredTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_UserIgnoredTags_UserId",
                table: "UserIgnoredTags",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWatchedTags_TagId",
                table: "UserWatchedTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWatchedTags_UserId",
                table: "UserWatchedTags",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Answers_AnswerId",
                table: "Comments",
                column: "AnswerId",
                principalTable: "Answers",
                principalColumn: "AnswerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_Answers_AnswerId",
                table: "Votes",
                column: "AnswerId",
                principalTable: "Answers",
                principalColumn: "AnswerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Answers_AnswerId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_Answers_AnswerId",
                table: "Votes");

            migrationBuilder.DropTable(
                name: "Answers");

            migrationBuilder.DropTable(
                name: "QuestionTag");

            migrationBuilder.DropTable(
                name: "UserIgnoredTags");

            migrationBuilder.DropTable(
                name: "UserWatchedTags");

            migrationBuilder.DropIndex(
                name: "IX_Votes_AnswerId",
                table: "Votes");

            migrationBuilder.DropIndex(
                name: "IX_Comments_AnswerId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "AnswerId",
                table: "Votes");

            migrationBuilder.DropColumn(
                name: "AnswerId",
                table: "Comments");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Votes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Votes",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<int>(
                name: "ViewCount",
                table: "Questions",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Questions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "Questions",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Questions",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "(getdate())");
        }
    }
}
