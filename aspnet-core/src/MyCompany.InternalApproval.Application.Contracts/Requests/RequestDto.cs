using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Requests;

// Thông tin đề xuất nội bộ trả về cho giao diện
public class RequestDto : FullAuditedEntityDto<Guid>
{
    // Tiêu đề đề xuất
    public string Title { get; set; } = string.Empty;

    // Nội dung mô tả chi tiết
    public string? Description { get; set; }

    // Loại đề xuất
    public RequestType Type { get; set; }

    // Trạng thái hiện tại
    public RequestStatus Status { get; set; }

    // Số tiền đề xuất (nếu có)
    public decimal? Amount { get; set; }

    // Ngày bắt đầu nghỉ phép
    public DateTime? StartDate { get; set; }

    // Ngày kết thúc nghỉ phép
    public DateTime? EndDate { get; set; }

    // Id người phê duyệt
    public Guid? ApproverId { get; set; }

    // Họ tên người phê duyệt
    public string? ApproverName { get; set; }

    // Thời gian phê duyệt
    public DateTime? ApprovalTime { get; set; }

    // Lý do từ chối
    public string? RejectionReason { get; set; }

    // Họ tên người tạo đề xuất
    public string? CreatorName { get; set; }

    // Tên phòng ban của người tạo
    public string? DepartmentName { get; set; }

    // Mã phòng ban của người tạo
    public string? DepartmentCode { get; set; }

    // Danh sách bình luận trao đổi
    public List<RequestCommentDto> Comments { get; set; } = new();
}
