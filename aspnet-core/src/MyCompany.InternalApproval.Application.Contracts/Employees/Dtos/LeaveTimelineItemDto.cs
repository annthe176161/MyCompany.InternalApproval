using System;
using MyCompany.InternalApproval.Requests;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// DTO thể hiện một đợt nghỉ phép cho giao diện biểu đồ/timeline nghỉ phép.
/// </summary>
public class LeaveTimelineItemDto
{
    public Guid RequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double Days { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovalTime { get; set; }
    public string? RejectionReason { get; set; }
}
