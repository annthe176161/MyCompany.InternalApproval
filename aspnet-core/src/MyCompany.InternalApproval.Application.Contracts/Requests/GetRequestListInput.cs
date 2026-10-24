using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Requests;

// Tham số lọc, tìm kiếm và phân trang danh sách đề xuất
public class GetRequestListInput : PagedAndSortedResultRequestDto
{
    // Từ khóa tìm kiếm theo tiêu đề hoặc mô tả
    public string? Filter { get; set; }

    // Lọc theo trạng thái đề xuất
    public RequestStatus? Status { get; set; }

    // Lọc theo loại đề xuất
    public RequestType? Type { get; set; }

    // Lọc theo người tạo đề xuất
    public Guid? CreatorId { get; set; }
}
