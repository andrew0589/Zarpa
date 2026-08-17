using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zarpa.Api.Migrations
{
    /// <inheritdoc />
    public partial class UserSelectedLicense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SelectedLicenseID",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SelectedLicenseID",
                table: "Users",
                column: "SelectedLicenseID");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Licenses_SelectedLicenseID",
                table: "Users",
                column: "SelectedLicenseID",
                principalTable: "Licenses",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Licenses_SelectedLicenseID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SelectedLicenseID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SelectedLicenseID",
                table: "Users");
        }
    }
}
