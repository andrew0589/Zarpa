using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NavigationES.Api.Migrations
{
    /// <inheritdoc />
    public partial class UserExamDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ExamDate",
                table: "Users",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExamDate",
                table: "Users");
        }
    }
}
