using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Employees;

// Tham số tìm kiếm, lọc và phân trang danh sách nhân viên
public class GetEmployeeListInput : PagedAndSortedResultRequestDto
{
    // Từ khóa tìm kiếm theo Họ tên, Tên đăng nhập, Email hoặc Vị trí
    public string? Filter { get; set; }

    // Lọc theo phòng ban cụ thể
    public Guid? DepartmentId { get; set; }

    // Lọc thâm niên tối thiểu (tháng)
    public int? MinTenureMonths { get; set; }

    // Lọc thâm niên tối đa (tháng)
    public int? MaxTenureMonths { get; set; }
}

