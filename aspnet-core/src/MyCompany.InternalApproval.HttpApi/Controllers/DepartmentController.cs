using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCompany.InternalApproval.Departments;
using MyCompany.InternalApproval.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Controllers;

// Controller cung cấp các API RESTful quản lý cơ cấu phòng ban (/api/app/department)
[Authorize]
[RemoteService(Name = "Default")]
[Route("api/app/department")]
public class DepartmentController : InternalApprovalController, IDepartmentAppService
{
    private readonly IDepartmentAppService _departmentAppService;

    public DepartmentController(IDepartmentAppService departmentAppService)
    {
        _departmentAppService = departmentAppService;
    }

    // Lấy danh sách phòng ban có phân trang và sắp xếp
    [HttpGet]
    public virtual Task<PagedResultDto<DepartmentDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        return _departmentAppService.GetListAsync(input);
    }

    // Lấy chi tiết một phòng ban theo Id
    [HttpGet("{id}")]
    [Authorize(InternalApprovalPermissions.Departments.Default)]
    public virtual Task<DepartmentDto> GetAsync(Guid id)
    {
        return _departmentAppService.GetAsync(id);
    }

    // Tạo mới phòng ban
    [HttpPost]
    [Authorize(InternalApprovalPermissions.Departments.Create)]
    public virtual Task<DepartmentDto> CreateAsync(CreateUpdateDepartmentDto input)
    {
        return _departmentAppService.CreateAsync(input);
    }

    // Cập nhật thông tin phòng ban
    [HttpPut("{id}")]
    [Authorize(InternalApprovalPermissions.Departments.Edit)]
    public virtual Task<DepartmentDto> UpdateAsync(Guid id, CreateUpdateDepartmentDto input)
    {
        return _departmentAppService.UpdateAsync(id, input);
    }

    // Xóa phòng ban
    [HttpDelete("{id}")]
    [Authorize(InternalApprovalPermissions.Departments.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _departmentAppService.DeleteAsync(id);
    }

    // Lấy danh sách thành viên thuộc phòng ban
    [HttpGet("members/{departmentId}")]
    public virtual Task<List<DepartmentMemberDto>> GetMembersAsync(Guid departmentId)
    {
        return _departmentAppService.GetMembersAsync(departmentId);
    }

    // Thêm nhân viên vào phòng ban
    [HttpPost("member/{departmentId}")]
    [Authorize(InternalApprovalPermissions.Departments.ManageMembers)]
    public virtual Task AddMemberAsync(Guid departmentId, [FromBody] AddDepartmentMemberDto input)
    {
        return _departmentAppService.AddMemberAsync(departmentId, input);
    }

    // Cập nhật chức danh hoặc ngày vào làm của thành viên trong phòng ban
    [HttpPut("member")]
    [Authorize(InternalApprovalPermissions.Departments.ManageMembers)]
    public virtual Task UpdateMemberAsync([FromQuery] Guid departmentId, [FromQuery] Guid userId, [FromBody] UpdateDepartmentMemberDto input)
    {
        return _departmentAppService.UpdateMemberAsync(departmentId, userId, input);
    }

    // Gỡ nhân viên khỏi phòng ban
    [HttpDelete("member")]
    [Authorize(InternalApprovalPermissions.Departments.ManageMembers)]
    public virtual Task RemoveMemberAsync([FromQuery] Guid departmentId, [FromQuery] Guid userId)
    {
        return _departmentAppService.RemoveMemberAsync(departmentId, userId);
    }

    // Bổ nhiệm hoặc hủy bổ nhiệm Trưởng phòng
    [HttpPost("set-manager")]
    [Authorize(InternalApprovalPermissions.Departments.ManageMembers)]
    public virtual Task SetManagerAsync([FromQuery] Guid departmentId, [FromQuery] Guid? userId)
    {
        return _departmentAppService.SetManagerAsync(departmentId, userId);
    }

    // Lấy danh sách người dùng khả dụng để gán vào phòng ban
    [HttpGet("available-users/{departmentId}")]
    public virtual Task<List<DepartmentUserLookupDto>> GetAvailableUsersAsync(Guid departmentId)
    {
        return _departmentAppService.GetAvailableUsersAsync(departmentId);
    }
}

