using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShitpostBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaTypeToImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Image_MediaType",
                table: "Post",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image_MediaType",
                table: "Post");
        }
    }
}
