using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Requests;

public class RequestDto : FullAuditedEntityDto<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RequestType Type { get; set; }
    public RequestStatus Status { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovalTime { get; set; }
    public string? RejectionReason { get; set; }
    public string? CreatorName { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentCode { get; set; }

    public List<RequestCommentDto> Comments { get; set; } = new();
}

