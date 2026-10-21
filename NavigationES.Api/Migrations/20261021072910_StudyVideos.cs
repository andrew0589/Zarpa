using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NavigationES.Api.Migrations
{
    /// <inheritdoc />
    public partial class StudyVideos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StudyDescription",
                table: "Topics",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudyImageUrl",
                table: "Topics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudyVideos",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicID = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VideoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubtitlesUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    IsFree = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    KeyTermsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChaptersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyVideos", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StudyVideos_Topics_TopicID",
                        column: x => x.TopicID,
                        principalTable: "Topics",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudyVideoProgress",
                columns: table => new
                {
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    VideoID = table.Column<long>(type: "bigint", nullable: false),
                    PositionSeconds = table.Column<int>(type: "int", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyVideoProgress", x => new { x.UserID, x.VideoID });
                    table.ForeignKey(
                        name: "FK_StudyVideoProgress_StudyVideos_VideoID",
                        column: x => x.VideoID,
                        principalTable: "StudyVideos",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyVideoProgress_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudyVideoProgress_VideoID",
                table: "StudyVideoProgress",
                column: "VideoID");

            migrationBuilder.CreateIndex(
                name: "IX_StudyVideos_TopicID_Code",
                table: "StudyVideos",
                columns: new[] { "TopicID", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudyVideoProgress");

            migrationBuilder.DropTable(
                name: "StudyVideos");

            migrationBuilder.DropColumn(
                name: "StudyDescription",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "StudyImageUrl",
                table: "Topics");
        }
    }
}
