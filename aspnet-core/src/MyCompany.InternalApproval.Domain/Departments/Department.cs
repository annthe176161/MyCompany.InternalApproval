using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Departments;

// Phòng ban
public class Department : FullAuditedAggregateRoot<Guid>
{
    // Mã phòng ban
    public string Code { get; private set; } = null!;

    // Tên phòng ban
    public string Name { get; private set; } = null!;

    // Mô tả
    public string? Description { get; private set; }

    // Id trưởng phòng
    public Guid? ManagerId { get; private set; }

    // Danh sách thành viên
    public ICollection<DepartmentMember> Members { get; private set; }

    protected Department()
    {
        Code = null!;
        Name = null!;
        Members = new Collection<DepartmentMember>();
    }

    public Department(
        Guid id,
        string code,
        string name,
        string? description = null,
        Guid? managerId = null) : base(id)
    {
        SetCode(code);
        SetName(name);
        Description = description;
        ManagerId = managerId;
        Members = new Collection<DepartmentMember>();
    }

    // Cập nhật mã phòng ban
    public Department SetCode(string code)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), maxLength: 50);
        return this;
    }

    // Cập nhật tên phòng ban
    public Department SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        return this;
    }

    // Cập nhật mô tả
    public Department SetDescription(string? description)
    {
        Description = description;
        return this;
    }

    // Gán trưởng phòng
    public Department SetManager(Guid? managerId)
    {
        ManagerId = managerId;
        return this;
    }
}
