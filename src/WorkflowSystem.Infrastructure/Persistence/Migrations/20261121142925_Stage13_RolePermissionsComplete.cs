using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WorkflowSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stage13_RolePermissionsComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000011") },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000011") },
                    { new Guid("00000000-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000011") },
                    { new Guid("00000000-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000011") },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000011") },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000011") },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000011") },
                    { new Guid("00000000-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000013") },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000013") },
                    { new Guid("00000000-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000013") },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000013") },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000013") },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000013") },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("00000000-0000-0000-0000-000000000013") },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000014") },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000014") },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000014") },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000014") },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new Guid("00000000-0000-0000-0000-000000000014") },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new Guid("00000000-0000-0000-0000-000000000014") },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000014") },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("00000000-0000-0000-0000-000000000014") },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000015") },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000015") },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000015") },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000015") },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new Guid("00000000-0000-0000-0000-000000000015") },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new Guid("00000000-0000-0000-0000-000000000015") },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000015") },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("00000000-0000-0000-0000-000000000015") },
                    { new Guid("00000000-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000016") },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000016") },
                    { new Guid("00000000-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000016") },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000016") },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000016") },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000016") },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000017") },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000017") },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000017") },
                    { new Guid("00000000-0000-0000-0000-000000000014"), new Guid("00000000-0000-0000-0000-000000000017") },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("00000000-0000-0000-0000-000000000017") },
                    { new Guid("00000000-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000014"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000015"), new Guid("00000000-0000-0000-0000-000000000018") },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("00000000-0000-0000-0000-000000000018") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000011") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000011") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000011") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000011") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000011") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000011") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000011") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000013") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000013") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000013") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000013") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000013") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000013") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("00000000-0000-0000-0000-000000000013") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000014") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000014") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000014") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000014") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000011"), new Guid("00000000-0000-0000-0000-000000000014") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000012"), new Guid("00000000-0000-0000-0000-000000000014") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000014") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("00000000-0000-0000-0000-000000000014") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000015") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000015") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000015") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000015") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000011"), new Guid("00000000-0000-0000-0000-000000000015") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000012"), new Guid("00000000-0000-0000-0000-000000000015") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000015") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("00000000-0000-0000-0000-000000000015") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000016") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000016") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000016") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000016") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000016") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000016") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000017") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000017") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000017") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000014"), new Guid("00000000-0000-0000-0000-000000000017") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("00000000-0000-0000-0000-000000000017") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000011"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000012"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000013"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000014"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000015"), new Guid("00000000-0000-0000-0000-000000000018") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000000-0000-0000-0000-000000000016"), new Guid("00000000-0000-0000-0000-000000000018") });
        }
    }
}
