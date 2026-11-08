using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NEXUS.Migrations
{
    /// <inheritdoc />
    public partial class updateTaskItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AssignedToUserId",
                table: "TaskItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiLink",
                table: "TaskItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingLink",
                table: "TaskItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_AssignedToUserId",
                table: "TaskItems",
                column: "AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_AspNetUsers_AssignedToUserId",
                table: "TaskItems",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_AspNetUsers_AssignedToUserId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_AssignedToUserId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ApiLink",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "MeetingLink",
                table: "TaskItems");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedToUserId",
                table: "TaskItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
