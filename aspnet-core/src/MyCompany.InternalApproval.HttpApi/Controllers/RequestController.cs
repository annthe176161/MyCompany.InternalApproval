using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCompany.InternalApproval.Permissions;
using MyCompany.InternalApproval.Requests;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace MyCompany.InternalApproval.Controllers;

/// <summary>
/// Controller API cung cấp các endpoint HTTP RESTful cho tính năng Quản lý Đề xuất (/api/app/request).
/// </summary>
[RemoteService(Name = "Default")]
[Route("api/app/request")]
[Authorize(InternalApprovalPermissions.Requests.Default)]
public class RequestController : InternalApprovalController, IRequestAppService
{
    private readonly IRequestAppService _requestAppService;

    /// <summary>
    /// Khởi tạo RequestController với RequestAppService.
    /// </summary>
    /// <param name="requestAppService">Dịch vụ ứng dụng xử lý nghiệp vụ đề xuất</param>
    public RequestController(IRequestAppService requestAppService)
    {
        _requestAppService = requestAppService;
    }

    /// <summary>
    /// API lấy danh sách đề xuất có phân trang, lọc và sắp xếp.
    /// GET /api/app/request
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<RequestDto>> GetListAsync(GetRequestListInput input)
    {
        return _requestAppService.GetListAsync(input);
    }

    /// <summary>
    /// API lấy thông tin chi tiết một đề xuất theo Id.
    /// GET /api/app/request/{id}
    /// </summary>
    [HttpGet("{id}")]
    public virtual Task<RequestDto> GetAsync(Guid id)
    {
        return _requestAppService.GetAsync(id);
    }

    /// <summary>
    /// API tạo mới một đề xuất ở trạng thái Nháp (Draft).
    /// POST /api/app/request
    /// </summary>
    [HttpPost]
    [Authorize(InternalApprovalPermissions.Requests.Create)]
    public virtual Task<RequestDto> CreateAsync(CreateRequestDto input)
    {
        return _requestAppService.CreateAsync(input);
    }

    /// <summary>
    /// API cập nhật thông tin đề xuất (chỉ áp dụng cho trạng thái Nháp và chính người tạo).
    /// PUT /api/app/request/{id}
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(InternalApprovalPermissions.Requests.Edit)]
    public virtual Task<RequestDto> UpdateAsync(Guid id, UpdateRequestDto input)
    {
        return _requestAppService.UpdateAsync(id, input);
    }

    /// <summary>
    /// API xóa đề xuất (chỉ áp dụng cho trạng thái Nháp và chính người tạo).
    /// DELETE /api/app/request/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(InternalApprovalPermissions.Requests.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _requestAppService.DeleteAsync(id);
    }

    /// <summary>
    /// API gửi duyệt đề xuất (chuyển trạng thái sang Đã gửi duyệt).
    /// POST /api/app/request/{id}/submit
    /// </summary>
    [HttpPost("{id}/submit")]
    public virtual Task<RequestDto> SubmitAsync(Guid id)
    {
        return _requestAppService.SubmitAsync(id);
    }

    /// <summary>
    /// API phê duyệt đề xuất.
    /// POST /api/app/request/{id}/approve
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(InternalApprovalPermissions.Requests.Approve)]
    public virtual Task<RequestDto> ApproveAsync(Guid id, ApproveRequestDto input)
    {
        return _requestAppService.ApproveAsync(id, input);
    }

    /// <summary>
    /// API từ chối đề xuất (bắt buộc nhập lý do từ chối).
    /// POST /api/app/request/{id}/reject
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(InternalApprovalPermissions.Requests.Reject)]
    public virtual Task<RequestDto> RejectAsync(Guid id, RejectRequestDto input)
    {
        return _requestAppService.RejectAsync(id, input);
    }

    /// <summary>
    /// API thêm bình luận vào đề xuất.
    /// POST /api/app/request/{id}/comment
    /// </summary>
    [HttpPost("{id}/comment")]
    public virtual Task<RequestCommentDto> AddCommentAsync(Guid id, CreateRequestCommentDto input)
    {
        return _requestAppService.AddCommentAsync(id, input);
    }
}
