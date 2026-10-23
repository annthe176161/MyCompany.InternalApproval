using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Requests;

/// <summary>
/// Thực thể Đề xuất Nội bộ (Request Aggregate Root).
/// Quản lý toàn bộ vòng đời của một phiếu đề xuất trong doanh nghiệp (Nghỉ phép, Thanh toán, Tạm ứng, Thiết bị...).
/// Hỗ trợ luồng kiểm soát trạng thái: Nháp -> Đã gửi duyệt -> Đã duyệt / Bị từ chối.
/// Kế thừa FullAuditedAggregateRoot để tự động theo dõi người tạo, người sửa, thời điểm tạo/sửa và xóa mềm.
/// </summary>
public class Request : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Tiêu đề ngắn gọn của đề xuất.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Nội dung chi tiết, lý do hoặc diễn giải mục đích đề xuất.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Phân loại đề xuất: Leave (Nghỉ phép), Payment (Thanh toán), Advance (Tạm ứng), Device (Thiết bị), Other (Khác).
    /// </summary>
    public RequestType Type { get; private set; }

    /// <summary>
    /// Trạng thái phê duyệt hiện tại: Draft (Nháp), Submitted (Chờ duyệt), Approved (Đã duyệt), Rejected (Từ chối).
    /// </summary>
    public RequestStatus Status { get; private set; }

    /// <summary>
    /// Số tiền yêu cầu đối với các loại đề xuất tài chính (Thanh toán / Tạm ứng).
    /// </summary>
    public decimal? Amount { get; private set; }

    /// <summary>
    /// Ngày bắt đầu áp dụng (dùng cho đề xuất nghỉ phép, công tác).
    /// </summary>
    public DateTime? StartDate { get; private set; }

    /// <summary>
    /// Ngày kết thúc áp dụng (dùng cho đề xuất nghỉ phép, công tác).
    /// </summary>
    public DateTime? EndDate { get; private set; }

    /// <summary>
    /// Id của người có thẩm quyền đã thực hiện phê duyệt hoặc từ chối đề xuất này.
    /// </summary>
    public Guid? ApproverId { get; private set; }

    /// <summary>
    /// Thời điểm chính xác mà đề xuất được phê duyệt hoặc từ chối.
    /// </summary>
    public DateTime? ApprovalTime { get; private set; }

    /// <summary>
    /// Lý do từ chối (bắt buộc nhập khi người duyệt quyết định từ chối đề xuất).
    /// </summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Danh sách bình luận, trao đổi phản hồi liên quan đến đề xuất này.
    /// </summary>
    public ICollection<RequestComment> Comments { get; private set; }

    /// <summary>
    /// Hàm khởi tạo mặc định cho ORM (EF Core).
    /// </summary>
    protected Request()
    {
        Title = null!;
        Comments = new Collection<RequestComment>();
    }

    /// <summary>
    /// Khởi tạo một đề xuất mới ở trạng thái mặc định ban đầu là Nháp (Draft).
    /// </summary>
    /// <param name="id">Mã khóa chính GUID</param>
    /// <param name="title">Tiêu đề đề xuất</param>
    /// <param name="type">Loại đề xuất</param>
    /// <param name="description">Diễn giải nội dung</param>
    /// <param name="amount">Số tiền đề xuất</param>
    /// <param name="startDate">Ngày bắt đầu</param>
    /// <param name="endDate">Ngày kết thúc</param>
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

    /// <summary>
    /// Thiết lập tiêu đề đề xuất, kiểm tra ràng buộc không rỗng và tối đa 200 ký tự.
    /// </summary>
    public Request SetTitle(string title)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 200);
        return this;
    }

    /// <summary>
    /// Cập nhật nội dung mô tả chi tiết của đề xuất.
    /// </summary>
    public Request SetDescription(string? description)
    {
        Description = description;
        return this;
    }

    /// <summary>
    /// Thay đổi phân loại nghiệp vụ của đề xuất.
    /// </summary>
    public Request SetType(RequestType type)
    {
        Type = type;
        return this;
    }

    /// <summary>
    /// Cập nhật số tiền với ràng buộc không được nhận giá trị âm.
    /// </summary>
    public Request SetAmount(decimal? amount)
    {
        if (amount.HasValue && amount.Value < 0)
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.AmountRequiredForType);
        }

        Amount = amount;
        return this;
    }

    /// <summary>
    /// Cập nhật khoảng thời gian nghỉ phép, kiểm tra ràng buộc ngày kết thúc không được trước ngày bắt đầu.
    /// </summary>
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

    /// <summary>
    /// Chuyển trạng thái đề xuất từ Nháp sang Đã gửi duyệt (Submitted).
    /// </summary>
    internal void Submit()
    {
        Status = RequestStatus.Submitted;
    }

    /// <summary>
    /// Phê duyệt đề xuất và ghi nhận người duyệt, thời điểm phê duyệt.
    /// </summary>
    /// <param name="approverId">Id người thực hiện phê duyệt</param>
    internal void Approve(Guid approverId)
    {
        Status = RequestStatus.Approved;
        ApproverId = approverId;
        ApprovalTime = DateTime.UtcNow;
        RejectionReason = null;
    }

    /// <summary>
    /// Từ chối đề xuất và lưu lại lý do từ chối để phản hồi cho người tạo.
    /// </summary>
    /// <param name="approverId">Id người thực hiện từ chối</param>
    /// <param name="reason">Lý do từ chối</param>
    internal void Reject(Guid approverId, string reason)
    {
        Status = RequestStatus.Rejected;
        ApproverId = approverId;
        ApprovalTime = DateTime.UtcNow;
        RejectionReason = reason;
    }

    /// <summary>
    /// Thêm bình luận hoặc phản hồi mới vào phiếu đề xuất.
    /// </summary>
    /// <param name="commentId">Khóa chính GUID của bình luận</param>
    /// <param name="content">Nội dung trao đổi</param>
    public RequestComment AddComment(Guid commentId, string content)
    {
        var comment = new RequestComment(commentId, Id, content);
        Comments.Add(comment);
        return comment;
    }
}
