using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNovelEntitiesForFullCRUD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Novels",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Novels",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Novels",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Novels",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "vi");

            migrationBuilder.AddColumn<string>(
                name: "OriginalSource",
                table: "Novels",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Rating",
                table: "Novels",
                type: "decimal(3,2)",
                nullable: false,
                defaultValue: 0.0m);

            migrationBuilder.AddColumn<int>(
                name: "RatingCount",
                table: "Novels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Novels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Novels",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Novels",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Novels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Chapters",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Chapters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Chapters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TranslatedById",
                table: "Chapters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranslatorNotes",
                table: "Chapters",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Chapters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Chapters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WordCount",
                table: "Chapters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "NovelRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NovelId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Review = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovelRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NovelRatings_Novels_NovelId",
                        column: x => x.NovelId,
                        principalTable: "Novels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NovelRatings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Huyền Huyễn" },
                    { 2, "Kiếm Hiệp" },
                    { 3, "Khoa Huyễn" },
                    { 4, "Lãng Mạn" },
                    { 5, "Trinh Thám" },
                    { 6, "Kinh Dị" },
                    { 7, "Hài Hước" },
                    { 8, "Đời Thường" }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 28, 6, 46, 30, 369, DateTimeKind.Utc).AddTicks(3014));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 28, 6, 46, 30, 369, DateTimeKind.Utc).AddTicks(3018));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 28, 6, 46, 30, 369, DateTimeKind.Utc).AddTicks(3021));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 28, 6, 46, 30, 369, DateTimeKind.Utc).AddTicks(3024));

            migrationBuilder.CreateIndex(
                name: "IX_Novels_CreatedAt",
                table: "Novels",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Novels_Rating",
                table: "Novels",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_Novels_Title",
                table: "Novels",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Novels_ViewCount",
                table: "Novels",
                column: "ViewCount");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CreatedAt",
                table: "Comments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_CreatedAt",
                table: "Chapters",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_NovelId_Number",
                table: "Chapters",
                columns: new[] { "NovelId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_Number",
                table: "Chapters",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_TranslatedById",
                table: "Chapters",
                column: "TranslatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NovelRatings_CreatedAt",
                table: "NovelRatings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NovelRatings_NovelId",
                table: "NovelRatings",
                column: "NovelId");

            migrationBuilder.CreateIndex(
                name: "IX_NovelRatings_NovelId_UserId",
                table: "NovelRatings",
                columns: new[] { "NovelId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NovelRatings_UserId",
                table: "NovelRatings",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Users_TranslatedById",
                table: "Chapters",
                column: "TranslatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Users_TranslatedById",
                table: "Chapters");

            migrationBuilder.DropTable(
                name: "NovelRatings");

            migrationBuilder.DropIndex(
                name: "IX_Novels_CreatedAt",
                table: "Novels");

            migrationBuilder.DropIndex(
                name: "IX_Novels_Rating",
                table: "Novels");

            migrationBuilder.DropIndex(
                name: "IX_Novels_Title",
                table: "Novels");

            migrationBuilder.DropIndex(
                name: "IX_Novels_ViewCount",
                table: "Novels");

            migrationBuilder.DropIndex(
                name: "IX_Comments_CreatedAt",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_CreatedAt",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_NovelId_Number",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_Number",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_TranslatedById",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "OriginalSource",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "RatingCount",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "TranslatedById",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "TranslatorNotes",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "WordCount",
                table: "Chapters");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 28, 6, 29, 22, 505, DateTimeKind.Utc).AddTicks(8554));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 28, 6, 29, 22, 505, DateTimeKind.Utc).AddTicks(8560));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 28, 6, 29, 22, 505, DateTimeKind.Utc).AddTicks(8563));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 28, 6, 29, 22, 505, DateTimeKind.Utc).AddTicks(8566));
        }
    }
}
