using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShitpostBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailySlopEntryUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DailySlopEntry_ChatGuildId_ChatChannelId_ChatMessageId",
                table: "DailySlopEntry",
                columns: new[] { "ChatGuildId", "ChatChannelId", "ChatMessageId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailySlopEntry_ChatGuildId_ChatChannelId_ChatMessageId",
                table: "DailySlopEntry"
            );
        }
    }
}
