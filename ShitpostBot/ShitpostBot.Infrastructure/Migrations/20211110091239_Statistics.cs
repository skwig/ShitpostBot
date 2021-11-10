using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShitpostBot.Infrastructure.Migrations
{
    public partial class Statistics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostUri",
                table: "Post");

            migrationBuilder.AddColumn<long>(
                name: "Statistics_MostSimilarTo_SimilarToPostId",
                table: "Post",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Statistics_MostSimilarTo_Similarity",
                table: "Post",
                type: "decimal(19,17)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Statistics_Placeholder",
                table: "Post",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Statistics_MostSimilarTo_SimilarToPostId",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "Statistics_MostSimilarTo_Similarity",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "Statistics_Placeholder",
                table: "Post");

            migrationBuilder.AddColumn<string>(
                name: "PostUri",
                table: "Post",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
