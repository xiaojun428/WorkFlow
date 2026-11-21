namespace WorkflowSystem.Domain.Enums;

/// <summary>
/// 文档状态（业务视角，与工作流运行状态分离）。
/// 列表筛选、业务查询一律使用此枚举，禁止依赖 Elsa 内部状态。
/// </summary>
public enum DocumentStatus
{
    /// <summary>草稿：未提交，无工作流实例。</summary>
    Draft = 0,

    /// <summary>待提交（预留，通常 Draft 直接 Submit）。</summary>
    Pending = 1,

    /// <summary>审批中：已提交，工作流运行中。</summary>
    InApproval = 2,

    /// <summary>已批准。</summary>
    Approved = 3,

    /// <summary>已驳回（Reject 后终止）。</summary>
    Rejected = 4,

    /// <summary>已退回（退回申请人修改）。</summary>
    Returned = 5,

    /// <summary>已撤回（申请人主动撤回）。</summary>
    Withdrawn = 6,

    /// <summary>已完成（流程正常结束）。</summary>
    Completed = 7,
}

/// <summary>
/// 工作流运行状态（引擎视角）。通过 IWorkflowService 与 Elsa 交互时使用。
/// </summary>
public enum WorkflowStatus
{
    NotStarted = 0,
    Running = 1,
    Suspended = 2,
    Completed = 3,
    Faulted = 4,
    Cancelled = 5,
}

/// <summary>
/// 审批任务状态。
/// </summary>
public enum ApprovalTaskStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Returned = 3,
    Withdrawn = 4,
    Cancelled = 5,
    Transferred = 6,
    Expired = 7,
}

/// <summary>
/// 审批动作类型（用于 ApprovalHistory 记录的事实）。
/// </summary>
public enum ApprovalAction
{
    Submitted = 0,
    Approved = 1,
    Rejected = 2,
    Returned = 3,
    ReturnToPrevious = 4,
    Withdrawn = 5,
    Transferred = 6,
    Delegated = 7,
    Expired = 8,
    Suspended = 9,
    Resumed = 10,
    Terminated = 11,
}

/// <summary>
/// 字段权限类型。
/// </summary>
public enum FieldPermissionType
{
    Hidden = 0,
    ReadOnly = 1,
    Editable = 2,
    Required = 3,
}

/// <summary>
/// 数据范围类型。
/// </summary>
public enum DataScopeType
{
    /// <summary>仅本人。</summary>
    Self = 0,

    /// <summary>本部门及子部门。</summary>
    Department = 1,

    /// <summary>全部。</summary>
    All = 2,
}

/// <summary>
/// 审批人指派类型。
/// </summary>
public enum AssignmentType
{
    /// <summary>指定用户。</summary>
    User = 0,

    /// <summary>指定角色。</summary>
    Role = 1,

    /// <summary>指定部门（含岗位）。</summary>
    Department = 2,
}

/// <summary>
/// 字段数据类型（用于动态字段值存储的强类型列选择）。
/// </summary>
public enum FieldType
{
    Text = 0,
    Number = 1,
    Date = 2,
    Boolean = 3,
    Amount = 4,
    Currency = 5,
}

/// <summary>
/// 邮件发送状态（Outbox）。
/// </summary>
public enum MailStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    DeadLetter = 3,
}

/// <summary>
/// 币种（ISO 4217 常用集合，可按需扩展）。
/// </summary>
public enum CurrencyCode
{
    CNY = 0,
    USD = 1,
    EUR = 2,
    JPY = 3,
    GBP = 4,
    HKD = 5,
}
