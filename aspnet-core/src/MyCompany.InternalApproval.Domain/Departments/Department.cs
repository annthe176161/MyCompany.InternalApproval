using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Departments;

/// <summary>
/// Thực thể Phòng ban (Department Aggregate Root).
/// Quản lý thông tin tổ chức, người đứng đầu và danh sách thành viên trong phòng ban.
/// Kế thừa FullAuditedAggregateRoot để tự động theo dõi lịch sử tạo, sửa, xóa mềm (Soft Delete).
/// </summary>
public class Department : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Mã định danh phòng ban (duy nhất, ví dụ: HR, IT, ACC, TECH).
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Tên phòng ban đầy đủ (ví dụ: Phòng Công nghệ Thông tin, Phòng Nhân sự).
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Mô tả chi tiết chức năng, nhiệm vụ của phòng ban.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Id của tài khoản Trưởng phòng (IdentityUser) phụ trách quản lý phê duyệt cấp phòng.
    /// </summary>
    public Guid? ManagerId { get; private set; }

    /// <summary>
    /// Danh sách các thành viên trực thuộc phòng ban.
    /// </summary>
    public ICollection<DepartmentMember> Members { get; private set; }

    /// <summary>
    /// Hàm khởi tạo mặc định cho ORM (EF Core).
    /// </summary>
    protected Department()
    {
        Code = null!;
        Name = null!;
        Members = new Collection<DepartmentMember>();
    }

    /// <summary>
    /// Khởi tạo một thực thể Phòng ban mới với các thông tin bắt buộc.
    /// </summary>
    /// <param name="id">Mã khóa chính GUID</param>
    /// <param name="code">Mã phòng ban</param>
    /// <param name="name">Tên phòng ban</param>
    /// <param name="description">Mô tả phòng ban (nếu có)</param>
    /// <param name="managerId">Id trưởng phòng (nếu có)</param>
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

    /// <summary>
    /// Cập nhật mã phòng ban với kiểm tra ràng buộc không rỗng và độ dài tối đa 50 ký tự.
    /// </summary>
    public Department SetCode(string code)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), maxLength: 50);
        return this;
    }

    /// <summary>
    /// Cập nhật tên phòng ban với kiểm tra ràng buộc không rỗng và độ dài tối đa 128 ký tự.
    /// </summary>
    public Department SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        return this;
    }

    /// <summary>
    /// Cập nhật thông tin mô tả phòng ban.
    /// </summary>
    public Department SetDescription(string? description)
    {
        Description = description;
        return this;
    }

    /// <summary>
    /// Chỉ định hoặc thay đổi Trưởng phòng phụ trách.
    /// </summary>
    public Department SetManager(Guid? managerId)
    {
        ManagerId = managerId;
        return this;
    }
}
