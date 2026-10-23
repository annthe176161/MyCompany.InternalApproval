using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// Tham số tìm kiếm, lọc và phân trang cho Báo cáo Thống kê Ngày nghỉ phép.
/// </summary>
public class GetLeaveReportInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Năm thống kê (mặc định là năm hiện tại).
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Lọc theo phòng ban cụ thể.
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Từ khóa tìm kiếm theo Họ tên, Tên đăng nhập, Email, hoặc Vị trí.
    /// </summary>
    public string? Filter { get; set; }
}
