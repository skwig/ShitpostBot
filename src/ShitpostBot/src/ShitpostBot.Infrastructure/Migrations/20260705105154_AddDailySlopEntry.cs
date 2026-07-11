using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShitpostBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailySlopEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailySlopEntry",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    PosterId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GameId = table.Column<string>(type: "text", nullable: false),
                    PostedOn = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ChatGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ChatChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ChatMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TrackedOn = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySlopEntry", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_DailySlopEntry_ChatGuildId_ChatChannelId_ChatMessageId",
                table: "DailySlopEntry",
                columns: new[] { "ChatGuildId", "ChatChannelId", "ChatMessageId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_DailySlopEntry_PostedOn",
                table: "DailySlopEntry",
                column: "PostedOn"
            );

            migrationBuilder.CreateIndex(
                name: "IX_DailySlopEntry_PosterId",
                table: "DailySlopEntry",
                column: "PosterId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DailySlopEntry");
        }
    }
}
