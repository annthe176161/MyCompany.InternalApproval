using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// DTO thông tin nhân viên hiển thị trong bảng danh sách nhân viên và tìm kiếm nhanh.
/// </summary>
public class EmployeeListItemDto : EntityDto<Guid>
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string Position { get; set; } = string.Empty;
    public bool IsManager { get; set; }

    /// <summary>
    /// Ngày bắt đầu làm việc hiệu lực: nếu JoinDate được lưu thì lấy JoinDate,
    /// nếu chưa có (null) thì tự động lấy CreationTime của tài khoản.
    /// </summary>
    public DateTime JoinDate { get; set; }

    /// <summary>
    /// Ngày vào làm được lưu trong bản ghi DepartmentMember (null nếu chưa thiết lập).
    /// </summary>
    public DateTime? RawJoinDate { get; set; }

    /// <summary>
    /// Ngày tạo tài khoản người dùng trên hệ thống.
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Thâm niên tính theo tổng số tháng.
    /// </summary>
    public int TenureMonths { get; set; }

    /// <summary>
    /// Thâm niên làm việc được định dạng tiếng Việt thân thiện ("2 năm 5 tháng", "8 tháng", "< 1 tháng", "1 năm").
    /// </summary>
    public string TenureFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Tổng số lượng đề xuất nhân viên gửi trong năm hiện tại.
    /// </summary>
    public int CurrentYearRequestCount { get; set; }
}
