using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyCompany.InternalApproval.Requests;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// Dịch vụ ứng dụng quản lý Hồ sơ Nhân viên và Thâm niên làm việc (Employee Profile & Tenure).
/// </summary>
public interface IEmployeeAppService : IApplicationService
{
    /// <summary>
    /// Lấy danh sách nhân viên có phân trang, tìm kiếm và lọc theo phòng ban, thâm niên.
    /// </summary>
    Task<PagedResultDto<EmployeeListItemDto>> GetListAsync(GetEmployeeListInput input);

    /// <summary>
    /// Lấy chi tiết hồ sơ nhân viên bao gồm thâm niên, thống kê năm hiện tại và đề xuất gần nhất.
    /// </summary>
    Task<EmployeeProfileDto> GetAsync(Guid id);

    /// <summary>
    /// Cập nhật ngày bắt đầu làm việc của nhân viên và tính toán lại thâm niên.
    /// </summary>
    Task<EmployeeProfileDto> UpdateJoinDateAsync(Guid id, UpdateEmployeeJoinDateDto input);

    /// <summary>
    /// Lấy lịch sử đề xuất của nhân viên kèm bộ lọc theo trạng thái, loại hình và năm.
    /// </summary>
    Task<PagedResultDto<RequestDto>> GetRequestsAsync(Guid id, GetEmployeeRequestsInput input);

    /// <summary>
    /// Lấy thống kê số liệu đề xuất theo năm của nhân viên.
    /// </summary>
    Task<EmployeeYearlyStatsDto> GetYearlyStatsAsync(Guid id, int? year = null);

    /// <summary>
    /// Lấy danh sách các đợt nghỉ phép đã bóc tách thời gian, số ngày và lý do phục vụ timeline nghỉ phép.
    /// </summary>
    Task<List<LeaveTimelineItemDto>> GetLeaveTimelineAsync(Guid id, int? year = null);

    /// <summary>
    /// Lấy dữ liệu phân tích và thống kê tổng thể nhân sự (phân bổ, thâm niên trung bình, tần suất đề xuất).
    /// </summary>
    Task<HrAnalyticsDto> GetAnalyticsAsync(int? year = null);

    /// <summary>
    /// Lấy báo cáo thống kê ngày nghỉ phép của nhân viên theo năm và phòng ban có phân trang, tìm kiếm.
    /// </summary>
    Task<PagedResultDto<EmployeeLeaveReportItemDto>> GetLeaveReportAsync(GetLeaveReportInput input);
}
