using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShitpostBot.Infrastructure.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Post",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    PostedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PostUri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChatMessageId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    PosterId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    TrackedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EvaluatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Post", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Post_PostedOn",
                table: "Post",
                column: "PostedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Post_PosterId",
                table: "Post",
                column: "PosterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Post");
        }
    }
}
