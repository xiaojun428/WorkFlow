namespace WorkflowSystem.Domain.Constants;

/// <summary>
/// 操作权限编码常量。与需求中的操作权限清单一一对应。
/// </summary>
public static class PermissionCodes
{
    public const string Create = "Create";
    public const string ViewOwn = "ViewOwn";
    public const string ViewDepartment = "ViewDepartment";
    public const string ViewAll = "ViewAll";
    public const string Edit = "Edit";
    public const string Delete = "Delete";
    public const string Submit = "Submit";
    public const string Withdraw = "Withdraw";
    public const string Approve = "Approve";
    public const string Reject = "Reject";
    public const string Return = "Return";
    public const string Transfer = "Transfer";
    public const string DownloadAttachment = "DownloadAttachment";
    public const string ManageWorkflow = "ManageWorkflow";
    public const string ManagePermission = "ManagePermission";
    public const string ViewAuditLog = "ViewAuditLog";
}

/// <summary>
/// 角色名称常量。
/// </summary>
public static class RoleNames
{
    public const string User = "User";
    public const string Approver = "Approver";
    public const string Manager = "Manager";
    public const string Finance = "Finance";
    public const string QA = "QA";
    public const string Purchasing = "Purchasing";
    public const string WorkflowAdmin = "WorkflowAdmin";
    public const string SystemAdmin = "SystemAdmin";
}

/// <summary>
/// 文档类型编码常量。
/// </summary>
public static class DocumentTypeCodes
{
    public const string Budget = "Budget";
    public const string EOL = "EOL";
    public const string Purchase = "Purchase";
    public const string Vendor = "Vendor";
}
