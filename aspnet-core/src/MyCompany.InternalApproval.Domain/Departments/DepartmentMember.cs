using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Departments;

// Thành viên thuộc phòng ban
public class DepartmentMember : CreationAuditedEntity<Guid>
{
    // Id phòng ban
    public Guid DepartmentId { get; private set; }

    // Id tài khoản người dùng
    public Guid UserId { get; private set; }

    // Chức vụ / vị trí trong phòng ban
    public string Position { get; private set; } = string.Empty;

    // Ngày bắt đầu làm việc tại công ty
    public DateTime? JoinDate { get; private set; }

    protected DepartmentMember()
    {
        Position = string.Empty;
    }

    public DepartmentMember(
        Guid id,
        Guid departmentId,
        Guid userId,
        string position,
        DateTime? joinDate = null) : base(id)
    {
        DepartmentId = departmentId;
        UserId = userId;
        SetPosition(position);
        SetJoinDate(joinDate);
    }

    // Cập nhật chức vụ / vị trí
    public DepartmentMember SetPosition(string position)
    {
        Position = Check.NotNullOrWhiteSpace(position, nameof(position), maxLength: 128);
        return this;
    }

    // Cập nhật ngày bắt đầu làm việc
    public DepartmentMember SetJoinDate(DateTime? joinDate)
    {
        JoinDate = joinDate?.Date;
        return this;
    }
}
