using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyCompany.InternalApproval.Requests;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MyCompany.InternalApproval.Employees;

// Giao diện dịch vụ quản lý Hồ sơ Nhân viên và Thâm niên làm việc
public interface IEmployeeAppService : IApplicationService
{
    // Lấy danh sách nhân viên có phân trang, tìm kiếm và lọc theo phòng ban, thâm niên
    Task<PagedResultDto<EmployeeListItemDto>> GetListAsync(GetEmployeeListInput input);

    // Lấy chi tiết hồ sơ nhân viên bao gồm thâm niên, thống kê năm hiện tại và đề xuất gần nhất
    Task<EmployeeProfileDto> GetAsync(Guid id);

    // Cập nhật ngày bắt đầu làm việc của nhân viên và tính toán lại thâm niên
    Task<EmployeeProfileDto> UpdateJoinDateAsync(Guid id, UpdateEmployeeJoinDateDto input);

    // Lấy lịch sử đề xuất của nhân viên kèm bộ lọc theo trạng thái, loại hình và năm
    Task<PagedResultDto<RequestDto>> GetRequestsAsync(Guid id, GetEmployeeRequestsInput input);

    // Lấy thống kê số liệu đề xuất theo năm của nhân viên
    Task<EmployeeYearlyStatsDto> GetYearlyStatsAsync(Guid id, int? year = null);

    // Lấy danh sách các đợt nghỉ phép đã bóc tách thời gian, số ngày và lý do phục vụ timeline nghỉ phép
    Task<List<LeaveTimelineItemDto>> GetLeaveTimelineAsync(Guid id, int? year = null);

    // Lấy dữ liệu phân tích và thống kê tổng thể nhân sự (phân bổ, thâm niên trung bình, tần suất đề xuất)
    Task<HrAnalyticsDto> GetAnalyticsAsync(int? year = null);

    // Lấy báo cáo thống kê ngày nghỉ phép của nhân viên theo năm và phòng ban có phân trang, tìm kiếm
    Task<PagedResultDto<EmployeeLeaveReportItemDto>> GetLeaveReportAsync(GetLeaveReportInput input);
}
