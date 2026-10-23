using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Departments;

// Thành viên phòng ban
public class DepartmentMember : CreationAuditedEntity<Guid>
{
    // Id phòng ban
    public Guid DepartmentId { get; set; }

    // Id nhân viên
    public Guid UserId { get; set; }

    // Vị trí / chức vụ
    public string Position { get; set; } = string.Empty;

    // Ngày vào công ty
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
