using System;
using Volo.Abp.EventBus;

namespace MyCompany.InternalApproval.Requests;

[EventName("MyCompany.InternalApproval.Requests.StatusChanged")]
public class RequestStatusChangedEto
{
    public Guid RequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public RequestStatus OldStatus { get; set; }
    public RequestStatus NewStatus { get; set; }
    public Guid? CreatorId { get; set; }
    public Guid? ApproverId { get; set; }
    public string? ApprovalNote { get; set; }
    public string? RejectionReason { get; set; }

    public RequestStatusChangedEto()
    {
    }

    public RequestStatusChangedEto(
        Guid requestId,
        string title,
        RequestStatus oldStatus,
        RequestStatus newStatus,
        Guid? creatorId,
        Guid? approverId = null,
        string? approvalNote = null,
        string? rejectionReason = null)
    {
        RequestId = requestId;
        Title = title;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        CreatorId = creatorId;
        ApproverId = approverId;
        ApprovalNote = approvalNote;
        RejectionReason = rejectionReason;
    }

    public RequestStatusChangedEto(
        Guid requestId,
        RequestStatus oldStatus,
        RequestStatus newStatus,
        Guid? creatorId,
        Guid? approverId = null,
        string? rejectionReason = null)
    {
        RequestId = requestId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        CreatorId = creatorId;
        ApproverId = approverId;
        RejectionReason = rejectionReason;
    }
}
