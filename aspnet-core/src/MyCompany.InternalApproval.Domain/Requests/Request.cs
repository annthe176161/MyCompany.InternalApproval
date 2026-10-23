using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Requests;

public class Request : FullAuditedAggregateRoot<Guid>
{
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public RequestType Type { get; private set; }
    public RequestStatus Status { get; private set; }
    public decimal? Amount { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public Guid? ApproverId { get; private set; }
    public DateTime? ApprovalTime { get; private set; }
    public string? RejectionReason { get; private set; }

    public ICollection<RequestComment> Comments { get; private set; }

    protected Request()
    {
        Title = null!;
        Comments = new Collection<RequestComment>();
    }

    public Request(
        Guid id,
        string title,
        RequestType type,
        string? description = null,
        decimal? amount = null,
        DateTime? startDate = null,
        DateTime? endDate = null) : base(id)
    {
        SetTitle(title);
        SetType(type);
        SetDescription(description);
        SetAmount(amount);
        SetDateRange(startDate, endDate);

        Status = RequestStatus.Draft;
        Comments = new Collection<RequestComment>();
    }

    public Request SetTitle(string title)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 200);
        return this;
    }

    public Request SetDescription(string? description)
    {
        Description = description;
        return this;
    }

    public Request SetType(RequestType type)
    {
        Type = type;
        return this;
    }

    public Request SetAmount(decimal? amount)
    {
        if (amount.HasValue && amount.Value < 0)
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.AmountRequiredForType);
        }

        Amount = amount;
        return this;
    }

    public Request SetDateRange(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.InvalidDateRange);
        }

        StartDate = startDate;
        EndDate = endDate;
        return this;
    }

    internal void Submit()
    {
        Status = RequestStatus.Submitted;
    }

    internal void Approve(Guid approverId)
    {
        Status = RequestStatus.Approved;
        ApproverId = approverId;
        ApprovalTime = DateTime.UtcNow;
        RejectionReason = null;
    }

    internal void Reject(Guid approverId, string reason)
    {
        Status = RequestStatus.Rejected;
        ApproverId = approverId;
        ApprovalTime = DateTime.UtcNow;
        RejectionReason = reason;
    }

    public RequestComment AddComment(Guid commentId, string content)
    {
        var comment = new RequestComment(commentId, Id, content);
        Comments.Add(comment);
        return comment;
    }
}
