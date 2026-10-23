using System;
using MyCompany.InternalApproval.Requests;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Notifications;

// Thông báo phê duyệt
public class ApprovalNotification : CreationAuditedEntity<Guid>
{
    // Id đề xuất
    public Guid RequestId { get; private set; }

    // Tiêu đề thông báo
    public string Title { get; private set; } = string.Empty;

    // Trạng thái đề xuất
    public RequestStatus Status { get; private set; }

    // Người nhận thông báo
    public Guid ReceiverId { get; private set; }

    // Người duyệt
    public Guid? ApproverId { get; private set; }

    // Nội dung thông báo
    public string Message { get; private set; } = string.Empty;

    // Trạng thái đã đọc
    public bool IsRead { get; private set; }

    protected ApprovalNotification()
    {
    }

    public ApprovalNotification(
        Guid id,
        Guid requestId,
        string title,
        RequestStatus status,
        Guid receiverId,
        Guid? approverId,
        string message) : base(id)
    {
        RequestId = requestId;
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 200);
        Status = status;
        ReceiverId = receiverId;
        ApproverId = approverId;
        Message = Check.NotNullOrWhiteSpace(message, nameof(message), maxLength: 2000);
        IsRead = false;
    }

    // Đánh dấu đã đọc
    public void MarkAsRead()
    {
        IsRead = true;
    }
}
