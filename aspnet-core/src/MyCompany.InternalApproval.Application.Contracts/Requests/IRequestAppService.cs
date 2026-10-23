using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MyCompany.InternalApproval.Requests;

public interface IRequestAppService : IApplicationService
{
    // Lấy danh sách đề xuất (có lọc và phân trang)
    Task<PagedResultDto<RequestDto>> GetListAsync(GetRequestListInput input);

    // Lấy chi tiết đề xuất theo Id
    Task<RequestDto> GetAsync(Guid id);

    // Tạo mới một đề xuất
    Task<RequestDto> CreateAsync(CreateRequestDto input);

    // Cập nhật thông tin đề xuất
    Task<RequestDto> UpdateAsync(Guid id, UpdateRequestDto input);

    // Xóa đề xuất
    Task DeleteAsync(Guid id);

    // Gửi duyệt đề xuất
    Task<RequestDto> SubmitAsync(Guid id);

    // Phê duyệt đề xuất
    Task<RequestDto> ApproveAsync(Guid id, ApproveRequestDto input);

    // Từ chối đề xuất
    Task<RequestDto> RejectAsync(Guid id, RejectRequestDto input);

    // Thêm bình luận vào đề xuất
    Task<RequestCommentDto> AddCommentAsync(Guid id, CreateRequestCommentDto input);
}
