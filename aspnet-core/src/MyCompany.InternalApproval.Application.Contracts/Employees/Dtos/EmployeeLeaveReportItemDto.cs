using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// DTO chứa thông tin thống kê số ngày nghỉ phép và các đợt nghỉ của từng nhân viên.
/// </summary>
public class EmployeeLeaveReportItemDto : EntityDto<Guid>
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Tổng số ngày nghỉ phép đã được duyệt trong năm.
    /// </summary>
    public double TotalApprovedLeaveDays { get; set; }

    /// <summary>
    /// Tổng số đợt nghỉ phép đã gửi duyệt trong năm (RequestType.Leave và Status != Draft).
    /// </summary>
    public int TotalSentLeaveRequests { get; set; }

    /// <summary>
    /// Tổng số đợt nghỉ phép đã được phê duyệt trong năm (RequestType.Leave và Status == Approved).
    /// </summary>
    public int TotalApprovedLeaveRequests { get; set; }

    /// <summary>
    /// Ngày bắt đầu của đợt nghỉ phép gần nhất.
    /// </summary>
    public DateTime? LatestLeaveStartDate { get; set; }

    /// <summary>
    /// Ngày kết thúc của đợt nghỉ phép gần nhất.
    /// </summary>
    public DateTime? LatestLeaveEndDate { get; set; }

    /// <summary>
    /// Số ngày nghỉ của đợt nghỉ phép gần nhất.
    /// </summary>
    public double? LatestLeaveDays { get; set; }

    /// <summary>
    /// Chuỗi định dạng hiển thị đợt nghỉ phép gần nhất (ví dụ: "25/10/2026 - 26/10/2026 (2 ngày)" hoặc "Chưa có").
    /// </summary>
    public string? LatestLeavePeriodFormatted { get; set; }
}
