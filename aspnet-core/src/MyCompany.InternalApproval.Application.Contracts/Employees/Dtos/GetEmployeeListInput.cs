using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// Tham số tìm kiếm, lọc và phân trang danh sách nhân viên.
/// </summary>
public class GetEmployeeListInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Từ khóa tìm kiếm theo Họ tên, Tên đăng nhập, Email, hoặc Vị trí.
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Lọc theo phòng ban cụ thể.
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Lọc thâm niên tối thiểu (tháng).
    /// </summary>
    public int? MinTenureMonths { get; set; }

    /// <summary>
    /// Lọc thâm niên tối đa (tháng).
    /// </summary>
    public int? MaxTenureMonths { get; set; }
}
