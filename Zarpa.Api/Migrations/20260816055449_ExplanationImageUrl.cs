using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zarpa.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExplanationImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Questions",
                newName: "ExplanationImageUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExplanationImageUrl",
                table: "Questions",
                newName: "ImageUrl");
        }
    }
}
