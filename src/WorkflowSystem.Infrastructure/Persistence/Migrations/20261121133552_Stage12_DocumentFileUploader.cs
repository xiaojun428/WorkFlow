using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stage12_DocumentFileUploader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DocumentFiles_UploadedByUserId",
                table: "DocumentFiles",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentFiles_Users_UploadedByUserId",
                table: "DocumentFiles",
                column: "UploadedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFiles_Users_UploadedByUserId",
                table: "DocumentFiles");

            migrationBuilder.DropIndex(
                name: "IX_DocumentFiles_UploadedByUserId",
                table: "DocumentFiles");
        }
    }
}
