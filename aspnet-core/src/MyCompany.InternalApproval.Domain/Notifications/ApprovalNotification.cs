using System;
using MyCompany.InternalApproval.Requests;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Notifications;

public class ApprovalNotification : CreationAuditedEntity<Guid>
{
    public Guid RequestId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public RequestStatus Status { get; private set; }
    public Guid ReceiverId { get; private set; }
    public Guid? ApproverId { get; private set; }
    public string Message { get; private set; } = string.Empty;
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

    public void MarkAsRead()
    {
        IsRead = true;
    }
}

