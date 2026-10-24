using System.Collections.Generic;
using MyCompany.InternalApproval.Requests;

namespace MyCompany.InternalApproval.Employees;

// Hồ sơ chi tiết của nhân viên bao gồm thâm niên, thống kê năm và đề xuất gần đây
public class EmployeeProfileDto : EmployeeListItemDto
{
    // Thống kê đề xuất trong năm hiện tại của nhân viên
    public EmployeeYearlyStatsDto CurrentYearStats { get; set; } = new();

    // Danh sách các đề xuất gần đây nhất của nhân viên
    public List<RequestDto> RecentRequests { get; set; } = new();
}

