using MyCompany.InternalApproval.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace MyCompany.InternalApproval.Permissions;

public class InternalApprovalPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var requestGroup = context.AddGroup(InternalApprovalPermissions.GroupName, L("Permission:InternalApproval"));

        var requestsPermission = requestGroup.AddPermission(
            InternalApprovalPermissions.Requests.Default,
            L("Permission:Requests"));

        requestsPermission.AddChild(
            InternalApprovalPermissions.Requests.Create,
            L("Permission:Requests.Create"));

        requestsPermission.AddChild(
            InternalApprovalPermissions.Requests.Edit,
            L("Permission:Requests.Edit"));

        requestsPermission.AddChild(
            InternalApprovalPermissions.Requests.Delete,
            L("Permission:Requests.Delete"));

        requestsPermission.AddChild(
            InternalApprovalPermissions.Requests.Approve,
            L("Permission:Requests.Approve"));

        requestsPermission.AddChild(
            InternalApprovalPermissions.Requests.Reject,
            L("Permission:Requests.Reject"));

        // Phân quyền Quản lý Phòng ban
        var departmentsPermission = requestGroup.AddPermission(
            InternalApprovalPermissions.Departments.Default,
            L("Permission:Departments"));

        departmentsPermission.AddChild(
            InternalApprovalPermissions.Departments.Create,
            L("Permission:Departments.Create"));

        departmentsPermission.AddChild(
            InternalApprovalPermissions.Departments.Edit,
            L("Permission:Departments.Edit"));

        departmentsPermission.AddChild(
            InternalApprovalPermissions.Departments.Delete,
            L("Permission:Departments.Delete"));

        departmentsPermission.AddChild(
            InternalApprovalPermissions.Departments.ManageMembers,
            L("Permission:Departments.ManageMembers"));

        // Phân quyền Quản lý Hồ sơ Nhân viên
        var employeesPermission = requestGroup.AddPermission(
            InternalApprovalPermissions.Employees.Default,
            L("Permission:Employees"));

        employeesPermission.AddChild(
            InternalApprovalPermissions.Employees.ViewAll,
            L("Permission:Employees.ViewAll"));

        employeesPermission.AddChild(
            InternalApprovalPermissions.Employees.Edit,
            L("Permission:Employees.Edit"));

        employeesPermission.AddChild(
            InternalApprovalPermissions.Employees.Analytics,
            L("Permission:Employees.Analytics"));

        // Loại bỏ các nhóm quyền không sử dụng (Multi-Tenancy & Features)
        try
        {
            var featureGroup = context.GetGroupOrNull("FeatureManagement");
            if (featureGroup != null)
            {
                context.RemoveGroup("FeatureManagement");
            }
        }
        catch { }

        try
        {
            var tenantGroup = context.GetGroupOrNull("AbpTenantManagement") ?? context.GetGroupOrNull("TenantManagement");
            if (tenantGroup != null)
            {
                context.RemoveGroup(tenantGroup.Name);
            }
        }
        catch { }
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<InternalApprovalResource>(name);
    }
}
