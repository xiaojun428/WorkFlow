using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Entities.Document;
using WorkflowSystem.Domain.Entities.Identity;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Domain.Entities.Permission;

/// <summary>
/// 字段级权限：针对某角色在某文档类型的某字段上的权限（Hidden/ReadOnly/Editable/Required）。
/// </summary>
public class FieldPermission : Entity
{
    public Guid RoleId { get; private set; }
    public Guid DocumentTypeId { get; private set; }
    public Guid FieldDefinitionId { get; private set; }
    public FieldPermissionType PermissionType { get; private set; }

    public Role? Role { get; private set; }
    public DocumentType? DocumentType { get; private set; }
    public DocumentFieldDefinition? FieldDefinition { get; private set; }

    private FieldPermission() { }

    public FieldPermission(Guid roleId, Guid documentTypeId, Guid fieldDefinitionId, FieldPermissionType permissionType)
    {
        RoleId = roleId;
        DocumentTypeId = documentTypeId;
        FieldDefinitionId = fieldDefinitionId;
        PermissionType = permissionType;
    }
}
