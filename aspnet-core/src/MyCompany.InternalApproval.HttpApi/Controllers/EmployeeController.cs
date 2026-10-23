using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCompany.InternalApproval.Employees;
using MyCompany.InternalApproval.Permissions;
using MyCompany.InternalApproval.Requests;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Controllers;

/// <summary>
/// Controller API cung cấp các endpoint HTTP RESTful cho module Quản lý Hồ sơ Nhân viên (/api/app/employee).
/// </summary>
[RemoteService(Name = "Default")]
[Route("api/app/employee")]
[Authorize(InternalApprovalPermissions.Employees.Default)]
public class EmployeeController : InternalApprovalController, IEmployeeAppService
{
    private readonly IEmployeeAppService _employeeAppService;

    public EmployeeController(IEmployeeAppService employeeAppService)
    {
        _employeeAppService = employeeAppService;
    }

    /// <summary>
    /// API lấy danh sách nhân viên có phân trang, tìm kiếm họ tên/email và lọc theo phòng ban, thâm niên.
    /// GET /api/app/employee
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<EmployeeListItemDto>> GetListAsync(GetEmployeeListInput input)
    {
        return _employeeAppService.GetListAsync(input);
    }

    /// <summary>
    /// API lấy chi tiết hồ sơ nhân viên bao gồm thâm niên, thống kê trong năm và đề xuất gần nhất.
    /// GET /api/app/employee/{id}
    /// </summary>
    [HttpGet("{id}")]
    public virtual Task<EmployeeProfileDto> GetAsync(Guid id)
    {
        return _employeeAppService.GetAsync(id);
    }

    /// <summary>
    /// API cập nhật ngày bắt đầu làm việc (JoinDate) của nhân viên và tính toán lại thâm niên.
    /// PUT /api/app/employee/{id}/join-date
    /// </summary>
    [HttpPut("{id}/join-date")]
    [Authorize(InternalApprovalPermissions.Employees.Edit)]
    public virtual Task<EmployeeProfileDto> UpdateJoinDateAsync(Guid id, UpdateEmployeeJoinDateDto input)
    {
        return _employeeAppService.UpdateJoinDateAsync(id, input);
    }

    /// <summary>
    /// API lấy lịch sử đề xuất của nhân viên kèm bộ lọc theo trạng thái, loại hình và năm.
    /// GET /api/app/employee/{id}/requests
    /// </summary>
    [HttpGet("{id}/requests")]
    public virtual Task<PagedResultDto<RequestDto>> GetRequestsAsync(Guid id, GetEmployeeRequestsInput input)
    {
        return _employeeAppService.GetRequestsAsync(id, input);
    }

    /// <summary>
    /// API lấy thống kê đề xuất theo năm của nhân viên.
    /// GET /api/app/employee/{id}/yearly-stats?year={year}
    /// </summary>
    [HttpGet("{id}/yearly-stats")]
    public virtual Task<EmployeeYearlyStatsDto> GetYearlyStatsAsync(Guid id, [FromQuery] int? year = null)
    {
        return _employeeAppService.GetYearlyStatsAsync(id, year);
    }

    /// <summary>
    /// API lấy danh sách các đợt nghỉ phép đã bóc tách thời gian, số ngày và lý do cho timeline nghỉ phép.
    /// GET /api/app/employee/{id}/leave-timeline?year={year}
    /// </summary>
    [HttpGet("{id}/leave-timeline")]
    public virtual Task<List<LeaveTimelineItemDto>> GetLeaveTimelineAsync(Guid id, [FromQuery] int? year = null)
    {
        return _employeeAppService.GetLeaveTimelineAsync(id, year);
    }

    /// <summary>
    /// API lấy dữ liệu phân tích và thống kê tổng thể nhân sự (phân bổ theo phòng ban, thâm niên trung bình, tần suất đề xuất).
    /// GET /api/app/employee/analytics?year={year}
    /// </summary>
    [HttpGet("analytics")]
    [Authorize(InternalApprovalPermissions.Employees.Analytics)]
    public virtual Task<HrAnalyticsDto> GetAnalyticsAsync([FromQuery] int? year = null)
    {
        return _employeeAppService.GetAnalyticsAsync(year);
    }

    /// <summary>
    /// API lấy báo cáo thống kê ngày nghỉ phép của nhân viên theo năm và phòng ban có phân trang, tìm kiếm.
    /// GET /api/app/employee/leave-report
    /// </summary>
    [HttpGet("leave-report")]
    public virtual Task<PagedResultDto<EmployeeLeaveReportItemDto>> GetLeaveReportAsync(GetLeaveReportInput input)
    {
        return _employeeAppService.GetLeaveReportAsync(input);
    }
}
