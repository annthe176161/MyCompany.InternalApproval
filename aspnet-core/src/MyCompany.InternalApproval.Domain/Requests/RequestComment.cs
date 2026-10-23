using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Requests;

// Bình luận đề xuất
public class RequestComment : CreationAuditedEntity<Guid>
{
    // Id đề xuất
    public Guid RequestId { get; private set; }

    // Nội dung bình luận
    public string Content { get; private set; } = null!;

    protected RequestComment()
    {
        Content = null!;
    }

    public RequestComment(Guid id, Guid requestId, string content) : base(id)
    {
        RequestId = requestId;
        SetContent(content);
    }

    // Cập nhật nội dung
    public void SetContent(string content)
    {
        Content = Check.NotNullOrWhiteSpace(content, nameof(content), maxLength: 2000);
    }
}
