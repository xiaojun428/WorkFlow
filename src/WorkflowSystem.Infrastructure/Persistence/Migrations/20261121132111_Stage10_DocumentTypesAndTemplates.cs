using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WorkflowSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stage10_DocumentTypesAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DocumentFieldDefinitions",
                columns: new[] { "Id", "DisplayName", "DisplayOrder", "DocumentTypeId", "FieldKey", "FieldType", "IsActive", "IsRequired" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000061"), "物料号", 1, new Guid("00000000-0000-0000-0000-000000000022"), "MaterialNo", 0, true, true },
                    { new Guid("00000000-0000-0000-0000-000000000062"), "EOL 原因", 2, new Guid("00000000-0000-0000-0000-000000000022"), "EolReason", 0, true, true },
                    { new Guid("00000000-0000-0000-0000-000000000063"), "EOL 生效日期", 3, new Guid("00000000-0000-0000-0000-000000000022"), "EffectiveDate", 2, true, true },
                    { new Guid("00000000-0000-0000-0000-000000000064"), "替代物料", 4, new Guid("00000000-0000-0000-0000-000000000022"), "ReplacementMaterial", 0, true, false },
                    { new Guid("00000000-0000-0000-0000-000000000065"), "库存处理方式", 5, new Guid("00000000-0000-0000-0000-000000000022"), "StockDisposal", 0, true, false },
                    { new Guid("00000000-0000-0000-0000-000000000071"), "采购金额", 1, new Guid("00000000-0000-0000-0000-000000000023"), "Amount", 4, true, true },
                    { new Guid("00000000-0000-0000-0000-000000000072"), "币种", 2, new Guid("00000000-0000-0000-0000-000000000023"), "Currency", 5, true, true },
                    { new Guid("00000000-0000-0000-0000-000000000073"), "供应商", 3, new Guid("00000000-0000-0000-0000-000000000023"), "Vendor", 0, true, true },
                    { new Guid("00000000-0000-0000-0000-000000000074"), "采购说明", 4, new Guid("00000000-0000-0000-0000-000000000023"), "Description", 0, true, false },
                    { new Guid("00000000-0000-0000-0000-000000000081"), "供应商名称", 1, new Guid("00000000-0000-0000-0000-000000000024"), "VendorName", 0, true, true },
                    { new Guid("00000000-0000-0000-0000-000000000082"), "统一社会信用代码", 2, new Guid("00000000-0000-0000-0000-000000000024"), "CreditCode", 0, true, true },
                    { new Guid("00000000-0000-0000-0000-000000000083"), "联系人", 3, new Guid("00000000-0000-0000-0000-000000000024"), "Contact", 0, true, true },
                    { new Guid("00000000-0000-0000-0000-000000000084"), "联系电话", 4, new Guid("00000000-0000-0000-0000-000000000024"), "Phone", 0, true, true },
                    { new Guid("00000000-0000-0000-0000-000000000085"), "供应商类型", 5, new Guid("00000000-0000-0000-0000-000000000024"), "VendorType", 0, true, false }
                });

            migrationBuilder.InsertData(
                table: "MailTemplates",
                columns: new[] { "Id", "BodyTemplate", "Code", "SubjectTemplate" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000091"), "您有一项待审批任务。\n文档编号：{{DocumentNo}}\n标题：{{Title}}\n审批步骤：{{StepName}}", "SubmitNotify", "【待审批】{{DocumentNo}} - {{StepName}}" },
                    { new Guid("00000000-0000-0000-0000-000000000092"), "您的文档「{{Title}}」已完成全部审批。", "ApprovalComplete", "审批完成：{{DocumentNo}}" },
                    { new Guid("00000000-0000-0000-0000-000000000093"), "您的文档「{{Title}}」已被驳回。\n意见：{{Comment}}", "ApprovalRejected", "审批驳回：{{DocumentNo}}" },
                    { new Guid("00000000-0000-0000-0000-000000000094"), "您有一项审批任务已超时未处理，请尽快处理。\n文档编号：{{DocumentNo}}\n审批步骤：{{StepName}}", "Reminder", "【催办】审批待处理：{{DocumentNo}} - {{StepName}}" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000061"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000062"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000063"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000064"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000065"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000071"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000072"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000073"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000074"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000081"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000082"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000083"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000084"));

            migrationBuilder.DeleteData(
                table: "DocumentFieldDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000085"));

            migrationBuilder.DeleteData(
                table: "MailTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000091"));

            migrationBuilder.DeleteData(
                table: "MailTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000092"));

            migrationBuilder.DeleteData(
                table: "MailTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000093"));

            migrationBuilder.DeleteData(
                table: "MailTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000094"));
        }
    }
}
