using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAIUsageEntityFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: false),
                    PromptTokens = table.Column<int>(type: "int", nullable: false),
                    CompletionTokens = table.Column<int>(type: "int", nullable: false),
                    ProcessingTimeMs = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PromptSessionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIUsages_PromptSessions_PromptSessionId",
                        column: x => x.PromptSessionId,
                        principalTable: "PromptSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AIUsages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 18, 7, 47, 18, 167, DateTimeKind.Utc).AddTicks(3053));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 18, 7, 47, 18, 167, DateTimeKind.Utc).AddTicks(3058));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 18, 7, 47, 18, 167, DateTimeKind.Utc).AddTicks(3062));

            migrationBuilder.CreateIndex(
                name: "IX_AIUsages_CreatedAt",
                table: "AIUsages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AIUsages_Model",
                table: "AIUsages",
                column: "Model");

            migrationBuilder.CreateIndex(
                name: "IX_AIUsages_PromptSessionId",
                table: "AIUsages",
                column: "PromptSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIUsages_UserId",
                table: "AIUsages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIUsages");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 18, 6, 24, 9, 399, DateTimeKind.Utc).AddTicks(8680));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 18, 6, 24, 9, 399, DateTimeKind.Utc).AddTicks(8684));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 7, 18, 6, 24, 9, 399, DateTimeKind.Utc).AddTicks(8687));
        }
    }
}
