namespace WorkflowSystem.Application.Workflows;

/// <summary>
/// 审批步骤定义：描述审批流程中的一个节点（StepKey、名称、指派方式、会签比例）。
/// 这是「流程定义」的业务模型，与 Elsa 引擎解耦；具体节点序列由 IApprovalFlowProvider 按文档类型 + 字段值解析。
/// </summary>
public sealed record ApprovalStepDefinition(
    string StepKey,
    string StepName,
    string? RoleName,
    /// <summary>会签比例（0-100）：候选人中需多少比例通过。100 表示单人/串行审批。</summary>
    int CountersignPercentage = 100);

/// <summary>
/// 审批流程定义提供者抽象：根据文档类型与文档字段值，解析出完整的审批步骤序列。
/// 这是金额条件分支、多级审批、会签规则的「可配置入口」——
/// 不同文档类型可提供不同实现，避免把业务分支硬编码进引擎或应用服务。
/// </summary>
public interface IApprovalFlowProvider
{
    /// <summary>
    /// 返回指定文档类型支持的流程提供者是否适用。
    /// </summary>
    bool Supports(string documentTypeCode);

    /// <summary>
    /// 解析审批步骤序列。
    /// </summary>
    /// <param name="documentTypeCode">文档类型编码。</param>
    /// <param name="fieldValues">文档动态字段值（键为 FieldKey，用于金额等条件判断）。</param>
    /// <returns>有序的审批步骤序列（先执行的在前）。</returns>
    Task<IReadOnlyList<ApprovalStepDefinition>> ResolveStepsAsync(
        string documentTypeCode,
        IReadOnlyDictionary<string, DocumentFieldValueSnapshot> fieldValues,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 文档字段值快照（用于流程条件判断，避免流程提供者直接依赖 EF 实体）。
/// </summary>
public sealed record DocumentFieldValueSnapshot(string FieldKey, decimal? Number, string? Text);
