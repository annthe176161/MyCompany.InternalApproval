using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCompany.InternalApproval.Permissions;
using MyCompany.InternalApproval.Requests;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Controllers;

// Controller cung cấp các API RESTful cho tính năng Quản lý Đề xuất (/api/app/request)
[RemoteService(Name = "Default")]
[Route("api/app/request")]
[Authorize(InternalApprovalPermissions.Requests.Default)]
public class RequestController : InternalApprovalController, IRequestAppService
{
    private readonly IRequestAppService _requestAppService;

    public RequestController(IRequestAppService requestAppService)
    {
        _requestAppService = requestAppService;
    }

    // Lấy danh sách đề xuất có phân trang, lọc và sắp xếp
    [HttpGet]
    public virtual Task<PagedResultDto<RequestDto>> GetListAsync(GetRequestListInput input)
    {
        return _requestAppService.GetListAsync(input);
    }

    // Lấy thông tin chi tiết một đề xuất theo Id
    [HttpGet("{id}")]
    public virtual Task<RequestDto> GetAsync(Guid id)
    {
        return _requestAppService.GetAsync(id);
    }

    // Tạo mới một đề xuất ở trạng thái Nháp
    [HttpPost]
    [Authorize(InternalApprovalPermissions.Requests.Create)]
    public virtual Task<RequestDto> CreateAsync(CreateRequestDto input)
    {
        return _requestAppService.CreateAsync(input);
    }

    // Cập nhật thông tin đề xuất ở trạng thái Nháp
    [HttpPut("{id}")]
    [Authorize(InternalApprovalPermissions.Requests.Edit)]
    public virtual Task<RequestDto> UpdateAsync(Guid id, UpdateRequestDto input)
    {
        return _requestAppService.UpdateAsync(id, input);
    }

    // Xóa đề xuất ở trạng thái Nháp
    [HttpDelete("{id}")]
    [Authorize(InternalApprovalPermissions.Requests.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _requestAppService.DeleteAsync(id);
    }

    // Gửi duyệt đề xuất
    [HttpPost("{id}/submit")]
    public virtual Task<RequestDto> SubmitAsync(Guid id)
    {
        return _requestAppService.SubmitAsync(id);
    }

    // Phê duyệt đề xuất
    [HttpPost("{id}/approve")]
    [Authorize(InternalApprovalPermissions.Requests.Approve)]
    public virtual Task<RequestDto> ApproveAsync(Guid id, ApproveRequestDto input)
    {
        return _requestAppService.ApproveAsync(id, input);
    }

    // Từ chối đề xuất (bắt buộc nhập lý do từ chối)
    [HttpPost("{id}/reject")]
    [Authorize(InternalApprovalPermissions.Requests.Reject)]
    public virtual Task<RequestDto> RejectAsync(Guid id, RejectRequestDto input)
    {
        return _requestAppService.RejectAsync(id, input);
    }

    // Thêm bình luận vào đề xuất
    [HttpPost("{id}/comment")]
    public virtual Task<RequestCommentDto> AddCommentAsync(Guid id, CreateRequestCommentDto input)
    {
        return _requestAppService.AddCommentAsync(id, input);
    }
}
