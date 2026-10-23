using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Requests;

public class RequestComment : CreationAuditedEntity<Guid>
{
    public Guid RequestId { get; private set; }
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

    public void SetContent(string content)
    {
        Content = Check.NotNullOrWhiteSpace(content, nameof(content), maxLength: 2000);
    }
}
