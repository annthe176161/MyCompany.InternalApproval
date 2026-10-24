using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Employees;

// Tham số tìm kiếm, lọc và phân trang cho Báo cáo Thống kê Ngày nghỉ phép
public class GetLeaveReportInput : PagedAndSortedResultRequestDto
{
    // Năm thống kê (mặc định là năm hiện tại)
    public int? Year { get; set; }

    // Lọc theo phòng ban cụ thể
    public Guid? DepartmentId { get; set; }

    // Từ khóa tìm kiếm theo Họ tên, Tên đăng nhập, Email hoặc Vị trí
    public string? Filter { get; set; }
}

