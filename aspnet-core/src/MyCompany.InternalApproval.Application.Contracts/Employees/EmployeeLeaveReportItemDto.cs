using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Employees;

// Thông tin thống kê số ngày nghỉ phép và các đợt nghỉ của từng nhân viên
public class EmployeeLeaveReportItemDto : EntityDto<Guid>
{
    // Tên đăng nhập
    public string UserName { get; set; } = string.Empty;

    // Họ và tên đầy đủ
    public string FullName { get; set; } = string.Empty;

    // Địa chỉ email
    public string Email { get; set; } = string.Empty;

    // Id phòng ban
    public Guid? DepartmentId { get; set; }

    // Mã phòng ban
    public string? DepartmentCode { get; set; }

    // Tên phòng ban
    public string? DepartmentName { get; set; }

    // Chức vụ / vị trí
    public string Position { get; set; } = string.Empty;

    // Tổng số ngày nghỉ phép đã được duyệt trong năm
    public double TotalApprovedLeaveDays { get; set; }

    // Tổng số đợt nghỉ phép đã gửi duyệt trong năm
    public int TotalSentLeaveRequests { get; set; }

    // Tổng số đợt nghỉ phép đã được phê duyệt trong năm
    public int TotalApprovedLeaveRequests { get; set; }

    // Ngày bắt đầu của đợt nghỉ phép gần nhất
    public DateTime? LatestLeaveStartDate { get; set; }

    // Ngày kết thúc của đợt nghỉ phép gần nhất
    public DateTime? LatestLeaveEndDate { get; set; }

    // Số ngày nghỉ của đợt nghỉ phép gần nhất
    public double? LatestLeaveDays { get; set; }

    // Chuỗi định dạng hiển thị đợt nghỉ phép gần nhất
    public string? LatestLeavePeriodFormatted { get; set; }
}

