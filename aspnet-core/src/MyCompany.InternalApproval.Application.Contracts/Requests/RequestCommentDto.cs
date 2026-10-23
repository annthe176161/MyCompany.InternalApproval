using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Requests;

public class RequestCommentDto : CreationAuditedEntityDto<Guid>
{
    public Guid RequestId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CreatorName { get; set; }
}

