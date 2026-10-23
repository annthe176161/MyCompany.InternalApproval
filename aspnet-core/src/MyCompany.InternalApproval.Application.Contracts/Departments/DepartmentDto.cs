using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Departments;

public class DepartmentDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public int MemberCount { get; set; }
}

