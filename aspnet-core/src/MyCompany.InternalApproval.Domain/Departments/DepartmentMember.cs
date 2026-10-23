using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Departments;

public class DepartmentMember : CreationAuditedEntity<Guid>
{
    public Guid DepartmentId { get; set; }
    public Guid UserId { get; set; }
    public string Position { get; set; } = string.Empty;
    public DateTime? JoinDate { get; set; }

    protected DepartmentMember()
    {
    }

    public DepartmentMember(Guid id, Guid departmentId, Guid userId, string position, DateTime? joinDate = null) : base(id)
    {
        DepartmentId = departmentId;
        UserId = userId;
        Position = position;
        JoinDate = joinDate;
    }
}

