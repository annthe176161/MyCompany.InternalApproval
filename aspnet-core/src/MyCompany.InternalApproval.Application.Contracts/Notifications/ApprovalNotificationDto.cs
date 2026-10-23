using System;
using MyCompany.InternalApproval.Requests;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Notifications;

/// <summary>
/// DTO chứa thông tin thông báo phê duyệt/từ chối gửi tới người dùng.
/// </summary>
public class ApprovalNotificationDto : EntityDto<Guid>
{
    /// <summary>
    /// Id của đề xuất liên quan.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Tiêu đề của đề xuất.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái phê duyệt (2: Đã duyệt, 3: Đã từ chối).
    /// </summary>
    public RequestStatus Status { get; set; }

    /// <summary>
    /// Id người nhận thông báo (người tạo đơn).
    /// </summary>
    public Guid ReceiverId { get; set; }

    /// <summary>
    /// Id người thực hiện duyệt/từ chối.
    /// </summary>
    public Guid? ApproverId { get; set; }

    /// <summary>
    /// Tên hiển thị của người duyệt/từ chối.
    /// </summary>
    public string? ApproverName { get; set; }

    /// <summary>
    /// Nội dung thông báo.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái đã đọc hay chưa.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Thời gian tạo thông báo.
    /// </summary>
    public DateTime CreationTime { get; set; }
}

