-- =====================================================================
-- 开发演示数据脚本（幂等，可重复执行）
-- 用途：为「文档审批工作流系统」补充可跑通审批流程的测试数据，
--       让 Bob（经理）登录后能在「待我审批」看到任务并验证批量审批。
--
-- 执行方式：
--   sqlcmd -S "localhost\MSSQLSERVER1" -d WorkflowSystem -i scripts\seed-demo-data.sql -E
--
-- 说明：
--   1. 所有 INSERT 均带 IF NOT EXISTS 判断，重复执行不产生重复数据。
--   2. 依赖 SeedData.cs 已通过 EF 迁移写入的固定 Guid（部门/角色/用户/文档类型/字段定义）。
--   3. 本脚本仅用于开发环境，生产环境禁止执行。
-- =====================================================================

SET NOCOUNT ON;

DECLARE @now DATETIME2 = SYSUTCDATETIME();

-- ---------------------------------------------------------------------
-- 1) 工作流定义 + 版本（与 Elsa 工作流 DefinitionId 对应：Budget/EOL/Purchase/Vendor）
--    审批任务需要引用 WorkflowDefinitionVersionId，故先幂等创建。
-- ---------------------------------------------------------------------

-- Budget 预算申请工作流
IF NOT EXISTS (SELECT 1 FROM WorkflowDefinitions WHERE Name = 'Budget')
BEGIN
    INSERT INTO WorkflowDefinitions (Id, Name, IsPublished) VALUES ('10000000-0000-0000-0000-000000000001', 'Budget', 1);
END
IF NOT EXISTS (SELECT 1 FROM WorkflowDefinitionVersions WHERE ElsaDefinitionId = 'Budget')
BEGIN
    INSERT INTO WorkflowDefinitionVersions (Id, WorkflowDefinitionId, VersionNo, ElsaDefinitionId, IsActive, CreatedAtUtc)
    VALUES ('11000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 1, 'Budget', 1, @now);
END

-- Purchase 采购申请工作流
IF NOT EXISTS (SELECT 1 FROM WorkflowDefinitions WHERE Name = 'Purchase')
BEGIN
    INSERT INTO WorkflowDefinitions (Id, Name, IsPublished) VALUES ('10000000-0000-0000-0000-000000000003', 'Purchase', 1);
END
IF NOT EXISTS (SELECT 1 FROM WorkflowDefinitionVersions WHERE ElsaDefinitionId = 'Purchase')
BEGIN
    INSERT INTO WorkflowDefinitionVersions (Id, WorkflowDefinitionId, VersionNo, ElsaDefinitionId, IsActive, CreatedAtUtc)
    VALUES ('11000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000003', 1, 'Purchase', 1, @now);
END

-- ---------------------------------------------------------------------
-- 2) 测试文档（InApproval 状态，申请人 Alice，预算申请类型）
-- ---------------------------------------------------------------------

-- 文档 1：预算申请（金额 5000，经理审批）
IF NOT EXISTS (SELECT 1 FROM Documents WHERE Id = '20000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO Documents (Id, DocumentNo, DocumentTypeId, Title, ApplicantUserId, OwnerDepartmentId,
                           DocumentStatus, WorkflowStatus, CurrentWorkflowStep, CurrentVersion,
                           SubmittedAtUtc, IsDeleted, CreatedAtUtc, UpdatedAtUtc)
    VALUES ('20000000-0000-0000-0000-000000000001',
            'BG-2026-0001',
            '00000000-0000-0000-0000-000000000021',  -- DocTypeBudget
            N'采购办公电脑预算申请',
            '00000000-0000-0000-0000-000000000031',  -- Alice
            '00000000-0000-0000-0000-000000000001',  -- DeptIsd
            2,      -- InApproval
            1,      -- Running
            'manager-approval',
            1,
            @now, 0, @now, @now);
END

-- 文档 1 字段值：金额 5000 CNY
IF NOT EXISTS (SELECT 1 FROM DocumentFieldValues WHERE DocumentId = '20000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO DocumentFieldValues (Id, DocumentId, FieldDefinitionId, FieldValueNumber, FieldValueText)
    VALUES
        ('21000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000051', 5000, NULL), -- Amount
        ('21000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000052', NULL, 'CNY'); -- Currency
END

-- 文档 2：预算申请（金额 50000，经理→部长审批）
IF NOT EXISTS (SELECT 1 FROM Documents WHERE Id = '20000000-0000-0000-0000-000000000002')
BEGIN
    INSERT INTO Documents (Id, DocumentNo, DocumentTypeId, Title, ApplicantUserId, OwnerDepartmentId,
                           DocumentStatus, WorkflowStatus, CurrentWorkflowStep, CurrentVersion,
                           SubmittedAtUtc, IsDeleted, CreatedAtUtc, UpdatedAtUtc)
    VALUES ('20000000-0000-0000-0000-000000000002',
            'BG-2026-0002',
            '00000000-0000-0000-0000-000000000021',
            N'服务器采购预算申请',
            '00000000-0000-0000-0000-000000000031',
            '00000000-0000-0000-0000-000000000001',
            2, 1, 'manager-approval', 1, @now, 0, @now, @now);
END

IF NOT EXISTS (SELECT 1 FROM DocumentFieldValues WHERE DocumentId = '20000000-0000-0000-0000-000000000002')
BEGIN
    INSERT INTO DocumentFieldValues (Id, DocumentId, FieldDefinitionId, FieldValueNumber, FieldValueText)
    VALUES
        ('21000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000051', 50000, NULL),
        ('21000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000052', NULL, 'CNY');
END

-- 文档 3：采购申请（金额 30000，采购申请类型）
IF NOT EXISTS (SELECT 1 FROM Documents WHERE Id = '20000000-0000-0000-0000-000000000003')
BEGIN
    INSERT INTO Documents (Id, DocumentNo, DocumentTypeId, Title, ApplicantUserId, OwnerDepartmentId,
                           DocumentStatus, WorkflowStatus, CurrentWorkflowStep, CurrentVersion,
                           SubmittedAtUtc, IsDeleted, CreatedAtUtc, UpdatedAtUtc)
    VALUES ('20000000-0000-0000-0000-000000000003',
            'PR-2026-0001',
            '00000000-0000-0000-0000-000000000023',  -- DocTypePurchase
            N'办公用品采购申请',
            '00000000-0000-0000-0000-000000000031',
            '00000000-0000-0000-0000-000000000001',
            2, 1, 'manager-approval', 1, @now, 0, @now, @now);
END

IF NOT EXISTS (SELECT 1 FROM DocumentFieldValues WHERE DocumentId = '20000000-0000-0000-0000-000000000003')
BEGIN
    INSERT INTO DocumentFieldValues (Id, DocumentId, FieldDefinitionId, FieldValueNumber, FieldValueText)
    VALUES
        ('21000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000071', 30000, NULL), -- 采购金额
        ('21000000-0000-0000-0000-000000000006', '20000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000072', NULL, 'CNY'); -- 币种
END

-- ---------------------------------------------------------------------
-- 3) 审批任务（Pending，指派给 Bob，经理审批步骤）+ 候选人
-- ---------------------------------------------------------------------

-- 任务 1：文档 1 的经理审批任务
IF NOT EXISTS (SELECT 1 FROM ApprovalTasks WHERE Id = '30000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO ApprovalTasks (Id, DocumentId, WorkflowDefinitionVersionId, StepKey, StepName,
                               AssignmentType, AssignedUserId, AssignedRoleId,
                               DueAtUtc, Status, CompletedApprovals, CountersignPercentage, RequiredApprovals)
    VALUES ('30000000-0000-0000-0000-000000000001',
            '20000000-0000-0000-0000-000000000001',
            '11000000-0000-0000-0000-000000000001',  -- Budget 工作流版本
            'manager-approval', N'经理审批',
            1,      -- AssignmentType.Role
            NULL,   -- AssignedUserId（按角色指派）
            '00000000-0000-0000-0000-000000000013',  -- RoleManager
            DATEADD(HOUR, 48, @now),
            0,      -- Pending
            0, 100, 1);
END

-- 任务 2：文档 2 的经理审批任务
IF NOT EXISTS (SELECT 1 FROM ApprovalTasks WHERE Id = '30000000-0000-0000-0000-000000000002')
BEGIN
    INSERT INTO ApprovalTasks (Id, DocumentId, WorkflowDefinitionVersionId, StepKey, StepName,
                               AssignmentType, AssignedUserId, AssignedRoleId,
                               DueAtUtc, Status, CompletedApprovals, CountersignPercentage, RequiredApprovals)
    VALUES ('30000000-0000-0000-0000-000000000002',
            '20000000-0000-0000-0000-000000000002',
            '11000000-0000-0000-0000-000000000001',
            'manager-approval', N'经理审批',
            1, NULL, '00000000-0000-0000-0000-000000000013',
            DATEADD(HOUR, 48, @now), 0, 0, 100, 1);
END

-- 任务 3：文档 3 的经理审批任务（采购申请）
IF NOT EXISTS (SELECT 1 FROM ApprovalTasks WHERE Id = '30000000-0000-0000-0000-000000000003')
BEGIN
    INSERT INTO ApprovalTasks (Id, DocumentId, WorkflowDefinitionVersionId, StepKey, StepName,
                               AssignmentType, AssignedUserId, AssignedRoleId,
                               DueAtUtc, Status, CompletedApprovals, CountersignPercentage, RequiredApprovals)
    VALUES ('30000000-0000-0000-0000-000000000003',
            '20000000-0000-0000-0000-000000000003',
            '11000000-0000-0000-0000-000000000003',  -- Purchase 工作流版本
            'manager-approval', N'经理审批',
            1, NULL, '00000000-0000-0000-0000-000000000013',
            DATEADD(HOUR, 48, @now), 0, 0, 100, 1);
END

-- ---------------------------------------------------------------------
-- 4) 审批候选人（Bob 作为候选人）
-- ---------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM ApprovalCandidates WHERE Id = '40000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO ApprovalCandidates (Id, ApprovalTaskId, CandidateUserId, HasActed, ActedStatus, ActedAtUtc)
    VALUES ('40000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001',
            '00000000-0000-0000-0000-000000000032',  -- Bob
            0, NULL, NULL);
END

IF NOT EXISTS (SELECT 1 FROM ApprovalCandidates WHERE Id = '40000000-0000-0000-0000-000000000002')
BEGIN
    INSERT INTO ApprovalCandidates (Id, ApprovalTaskId, CandidateUserId, HasActed, ActedStatus, ActedAtUtc)
    VALUES ('40000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000002',
            '00000000-0000-0000-0000-000000000032',
            0, NULL, NULL);
END

IF NOT EXISTS (SELECT 1 FROM ApprovalCandidates WHERE Id = '40000000-0000-0000-0000-000000000003')
BEGIN
    INSERT INTO ApprovalCandidates (Id, ApprovalTaskId, CandidateUserId, HasActed, ActedStatus, ActedAtUtc)
    VALUES ('40000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000003',
            '00000000-0000-0000-0000-000000000032',
            0, NULL, NULL);
END

PRINT '演示数据补充完成：3 条待审批文档 + 3 个待审批任务 + 3 个候选人（Bob）。';
