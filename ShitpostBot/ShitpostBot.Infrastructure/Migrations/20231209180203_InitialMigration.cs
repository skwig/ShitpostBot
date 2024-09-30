using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;
using ShitpostBot.Domain;

#nullable disable

namespace ShitpostBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

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
                    Image_ImageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Image_ImageUri = table.Column<string>(type: "text", nullable: true),
                    Image_ImageFeatures_FeatureVector = table.Column<Vector>(type: "vector", nullable: true),
                    Link_LinkId = table.Column<string>(type: "text", nullable: true),
                    Link_LinkUri = table.Column<string>(type: "text", nullable: true),
                    Link_LinkProvider = table.Column<int>(type: "integer", nullable: true)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Post");
        }
    }
}
