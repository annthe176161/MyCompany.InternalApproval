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

// Controller cung cấp các API RESTful cho module Quản lý Hồ sơ Nhân viên (/api/app/employee)
[RemoteService(Name = "Default")]
[Route("api/app/employee")]
[Authorize]
public class EmployeeController : InternalApprovalController, IEmployeeAppService
{
    private readonly IEmployeeAppService _employeeAppService;

    public EmployeeController(IEmployeeAppService employeeAppService)
    {
        _employeeAppService = employeeAppService;
    }

    // Lấy danh sách nhân viên có phân trang, tìm kiếm họ tên/email và lọc theo phòng ban, thâm niên
    [HttpGet]
    public virtual Task<PagedResultDto<EmployeeListItemDto>> GetListAsync(GetEmployeeListInput input)
    {
        return _employeeAppService.GetListAsync(input);
    }

    // Lấy chi tiết hồ sơ nhân viên bao gồm thâm niên, thống kê trong năm và đề xuất gần nhất
    [HttpGet("{id}")]
    public virtual Task<EmployeeProfileDto> GetAsync(Guid id)
    {
        return _employeeAppService.GetAsync(id);
    }

    // Cập nhật ngày bắt đầu làm việc của nhân viên và tính toán lại thâm niên
    [HttpPut("{id}/join-date")]
    [Authorize(InternalApprovalPermissions.Employees.Edit)]
    public virtual Task<EmployeeProfileDto> UpdateJoinDateAsync(Guid id, UpdateEmployeeJoinDateDto input)
    {
        return _employeeAppService.UpdateJoinDateAsync(id, input);
    }

    // Lấy lịch sử đề xuất của nhân viên kèm bộ lọc theo trạng thái, loại hình và năm
    [HttpGet("{id}/requests")]
    public virtual Task<PagedResultDto<RequestDto>> GetRequestsAsync(Guid id, GetEmployeeRequestsInput input)
    {
        return _employeeAppService.GetRequestsAsync(id, input);
    }

    // Lấy thống kê đề xuất theo năm của nhân viên
    [HttpGet("{id}/yearly-stats")]
    public virtual Task<EmployeeYearlyStatsDto> GetYearlyStatsAsync(Guid id, [FromQuery] int? year = null)
    {
        return _employeeAppService.GetYearlyStatsAsync(id, year);
    }

    // Lấy danh sách các đợt nghỉ phép đã bóc tách thời gian, số ngày và lý do cho timeline nghỉ phép
    [HttpGet("{id}/leave-timeline")]
    public virtual Task<List<LeaveTimelineItemDto>> GetLeaveTimelineAsync(Guid id, [FromQuery] int? year = null)
    {
        return _employeeAppService.GetLeaveTimelineAsync(id, year);
    }

    // Lấy dữ liệu phân tích và thống kê tổng thể nhân sự
    [HttpGet("analytics")]
    [Authorize]
    public virtual Task<HrAnalyticsDto> GetAnalyticsAsync([FromQuery] int? year = null)
    {
        return _employeeAppService.GetAnalyticsAsync(year);
    }

    // Lấy báo cáo thống kê ngày nghỉ phép của nhân viên theo năm và phòng ban có phân trang, tìm kiếm
    [HttpGet("leave-report")]
    public virtual Task<PagedResultDto<EmployeeLeaveReportItemDto>> GetLeaveReportAsync(GetLeaveReportInput input)
    {
        return _employeeAppService.GetLeaveReportAsync(input);
    }
}
