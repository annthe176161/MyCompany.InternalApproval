using System;
using Volo.Abp.BackgroundJobs;

namespace MyCompany.InternalApproval.Requests;

[BackgroundJobName("SendApprovalNotification")]
public class SendApprovalNotificationArgs
{
    public Guid RequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public Guid? CreatorId { get; set; }
    public Guid? ApproverId { get; set; }
    public string? ApprovalNote { get; set; }
    public string? RejectionReason { get; set; }
}

