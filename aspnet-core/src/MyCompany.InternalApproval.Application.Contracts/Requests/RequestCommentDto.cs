using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Requests;

// Thông tin bình luận trong đề xuất
public class RequestCommentDto : CreationAuditedEntityDto<Guid>
{
    // Id đề xuất
    public Guid RequestId { get; set; }

    // Nội dung bình luận
    public string Content { get; set; } = string.Empty;

    // Tên người viết bình luận
    public string? CreatorName { get; set; }
}
