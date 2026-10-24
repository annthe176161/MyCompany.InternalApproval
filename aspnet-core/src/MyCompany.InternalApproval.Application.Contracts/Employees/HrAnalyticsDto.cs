using System.Collections.Generic;

namespace MyCompany.InternalApproval.Employees;

// Dữ liệu phân tích và thống kê tổng thể nhân sự toàn công ty
public class HrAnalyticsDto
{
    // Năm thống kê
    public int Year { get; set; }

    // Tổng quan số liệu nhân sự toàn công ty
    public CompanyHrSummaryDto CompanySummary { get; set; } = new();

    // Phân bổ quân số theo từng phòng ban
    public List<DepartmentHeadcountDto> HeadcountByDepartment { get; set; } = new();

    // Thống kê thâm niên trung bình theo từng phòng ban
    public List<DepartmentTenureDto> TenureByDepartment { get; set; } = new();

    // Thống kê tần suất gửi đề xuất theo từng phòng ban
    public List<DepartmentRequestFrequencyDto> RequestFrequencyByDepartment { get; set; } = new();

    // Danh sách nhân viên gửi nhiều đề xuất nhất
    public List<TopRequesterDto> TopRequesters { get; set; } = new();
}

