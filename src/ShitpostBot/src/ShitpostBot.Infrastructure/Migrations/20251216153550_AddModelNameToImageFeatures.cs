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
            // Add column as nullable initially
            migrationBuilder.AddColumn<string>(
                name: "Image_ImageFeatures_ModelName",
                table: "Post",
                type: "text",
                nullable: true);
            
            // Populate existing rows with "legacy" model name
            migrationBuilder.Sql(
                @"UPDATE ""Post"" 
                  SET ""Image_ImageFeatures_ModelName"" = 'legacy' 
                  WHERE ""Image_ImageFeatures_FeatureVector"" IS NOT NULL");
            
            // Make column required (non-nullable)
            migrationBuilder.AlterColumn<string>(
                name: "Image_ImageFeatures_ModelName",
                table: "Post",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
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
