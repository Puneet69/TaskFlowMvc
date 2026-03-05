using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlowMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixAttachmentDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileAttachments_TaskItems_TaskItemId",
                table: "FileAttachments");

            migrationBuilder.AddForeignKey(
                name: "FK_FileAttachments_TaskItems_TaskItemId",
                table: "FileAttachments",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileAttachments_TaskItems_TaskItemId",
                table: "FileAttachments");

            migrationBuilder.AddForeignKey(
                name: "FK_FileAttachments_TaskItems_TaskItemId",
                table: "FileAttachments",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
