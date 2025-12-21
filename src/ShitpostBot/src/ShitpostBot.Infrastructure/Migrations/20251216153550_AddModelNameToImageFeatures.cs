using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShitpostBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModelNameToImageFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Image_ImageFeatures_ModelName",
                table: "Post",
                type: "text",
                nullable: true);
            
            migrationBuilder.Sql(
                @"UPDATE ""Post"" 
                  SET ""Image_ImageFeatures_ModelName"" = 'legacy' 
                  WHERE ""Image_ImageFeatures_FeatureVector"" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image_ImageFeatures_ModelName",
                table: "Post");
        }
    }
}
