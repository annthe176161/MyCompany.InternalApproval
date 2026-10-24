using System;
using MyCompany.InternalApproval.Requests;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Notifications;

// Thông tin thông báo phê duyệt hoặc từ chối gửi tới người dùng
public class ApprovalNotificationDto : EntityDto<Guid>
{
    // Id của đề xuất liên quan
    public Guid RequestId { get; set; }

    // Tiêu đề của đề xuất
    public string Title { get; set; } = string.Empty;

    // Trạng thái phê duyệt
    public RequestStatus Status { get; set; }

    // Id người nhận thông báo
    public Guid ReceiverId { get; set; }

    // Id người thực hiện duyệt hoặc từ chối
    public Guid? ApproverId { get; set; }

    // Tên hiển thị của người duyệt hoặc từ chối
    public string? ApproverName { get; set; }

    // Nội dung chi tiết thông báo
    public string Message { get; set; } = string.Empty;

    // Trạng thái đã đọc hay chưa
    public bool IsRead { get; set; }

    // Thời gian tạo thông báo
    public DateTime CreationTime { get; set; }
}
