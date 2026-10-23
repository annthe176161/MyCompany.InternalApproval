using System.Collections.Generic;
using MyCompany.InternalApproval.Requests;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// DTO hồ sơ chi tiết của nhân viên, bao gồm thông tin cá nhân, thâm niên,
/// thống kê số liệu trong năm và danh sách đề xuất gần đây.
/// </summary>
public class EmployeeProfileDto : EmployeeListItemDto
{
    /// <summary>
    /// Thống kê đề xuất trong năm hiện tại của nhân viên.
    /// </summary>
    public EmployeeYearlyStatsDto CurrentYearStats { get; set; } = new();

    /// <summary>
    /// Danh sách các đề xuất gần đây nhất của nhân viên.
    /// </summary>
    public List<RequestDto> RecentRequests { get; set; } = new();
}
