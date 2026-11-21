using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stage8_ApprovalEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletedApprovals",
                table: "ApprovalTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CountersignPercentage",
                table: "ApprovalTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreviousStepKey",
                table: "ApprovalTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovals",
                table: "ApprovalTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActedAtUtc",
                table: "ApprovalCandidates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActedStatus",
                table: "ApprovalCandidates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasActed",
                table: "ApprovalCandidates",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedApprovals",
                table: "ApprovalTasks");

            migrationBuilder.DropColumn(
                name: "CountersignPercentage",
                table: "ApprovalTasks");

            migrationBuilder.DropColumn(
                name: "PreviousStepKey",
                table: "ApprovalTasks");

            migrationBuilder.DropColumn(
                name: "RequiredApprovals",
                table: "ApprovalTasks");

            migrationBuilder.DropColumn(
                name: "ActedAtUtc",
                table: "ApprovalCandidates");

            migrationBuilder.DropColumn(
                name: "ActedStatus",
                table: "ApprovalCandidates");

            migrationBuilder.DropColumn(
                name: "HasActed",
                table: "ApprovalCandidates");
        }
    }
}
