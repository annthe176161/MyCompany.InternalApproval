using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Requests;

// Đề xuất nội bộ
public class Request : FullAuditedAggregateRoot<Guid>
{
    // Tiêu đề
    public string Title { get; private set; } = null!;

    // Mô tả chi tiết
    public string? Description { get; private set; }

    // Loại đề xuất (nghỉ phép, thanh toán, ...)
    public RequestType Type { get; private set; }

    // Trạng thái (nháp, chờ duyệt, đã duyệt, từ chối)
    public RequestStatus Status { get; private set; }

    // Số tiền (nếu có)
    public decimal? Amount { get; private set; }

    // Ngày bắt đầu
    public DateTime? StartDate { get; private set; }

    // Ngày kết thúc
    public DateTime? EndDate { get; private set; }

    // Người duyệt
    public Guid? ApproverId { get; private set; }

    // Thời gian duyệt
    public DateTime? ApprovalTime { get; private set; }

    // Lý do từ chối
    public string? RejectionReason { get; private set; }

    // Danh sách bình luận
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

    // Đổi tiêu đề
    public Request SetTitle(string title)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 200);
        return this;
    }

    // Đổi mô tả
    public Request SetDescription(string? description)
    {
        Description = description;
        return this;
    }

    // Đổi loại đề xuất
    public Request SetType(RequestType type)
    {
        Type = type;
        return this;
    }

    // Cập nhật số tiền
    public Request SetAmount(decimal? amount)
    {
        if (amount.HasValue && amount.Value < 0)
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.AmountRequiredForType);
        }

        Amount = amount;
        return this;
    }

    // Cập nhật khoảng thời gian
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

    // Gửi duyệt
    internal void Submit()
    {
        Status = RequestStatus.Submitted;
    }

    // Phê duyệt
    internal void Approve(Guid approverId)
    {
        Status = RequestStatus.Approved;
        ApproverId = approverId;
        ApprovalTime = DateTime.UtcNow;
        RejectionReason = null;
    }

    // Từ chối
    internal void Reject(Guid approverId, string reason)
    {
        Status = RequestStatus.Rejected;
        ApproverId = approverId;
        ApprovalTime = DateTime.UtcNow;
        RejectionReason = reason;
    }

    // Thêm bình luận
    public RequestComment AddComment(Guid commentId, string content)
    {
        var comment = new RequestComment(commentId, Id, content);
        Comments.Add(comment);
        return comment;
    }
}
