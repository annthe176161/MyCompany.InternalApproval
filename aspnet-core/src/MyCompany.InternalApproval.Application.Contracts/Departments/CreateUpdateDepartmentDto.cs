using System;
using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Departments;

// Dữ liệu đầu vào khi tạo mới hoặc cập nhật phòng ban
public class CreateUpdateDepartmentDto
{
    // Mã phòng ban
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    // Tên phòng ban
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    // Mô tả phòng ban
    [MaxLength(500)]
    public string? Description { get; set; }

    // Id trưởng phòng
    public Guid? ManagerId { get; set; }
}
