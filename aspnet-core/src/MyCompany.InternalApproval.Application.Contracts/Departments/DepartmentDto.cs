using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Departments;

// Thông tin phòng ban trả về cho giao diện
public class DepartmentDto : FullAuditedEntityDto<Guid>
{
    // Mã phòng ban
    public string Code { get; set; } = string.Empty;

    // Tên phòng ban
    public string Name { get; set; } = string.Empty;

    // Mô tả phòng ban
    public string? Description { get; set; }

    // Id trưởng phòng
    public Guid? ManagerId { get; set; }

    // Họ tên trưởng phòng
    public string? ManagerName { get; set; }

    // Số lượng thành viên thuộc phòng ban
    public int MemberCount { get; set; }
}
