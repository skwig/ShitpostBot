using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PostedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChatGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ChatChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ChatMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    PosterId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TrackedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EvaluatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Statistics_MostSimilarTo_SimilarToPostId = table.Column<long>(type: "bigint", nullable: true),
                    Statistics_MostSimilarTo_Similarity = table.Column<decimal>(type: "numeric(19,17)", nullable: true),
                    Statistics_Placeholder = table.Column<bool>(type: "boolean", nullable: true)
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
