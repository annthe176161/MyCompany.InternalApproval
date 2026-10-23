using System;
using MyCompany.InternalApproval.Requests;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Notifications;

/// <summary>
/// Thực thể Thông báo Phê duyệt (ApprovalNotification Entity).
/// Gửi thông báo đến người liên quan khi trạng thái đề xuất thay đổi (được duyệt hoặc bị từ chối).
/// Kế thừa CreationAuditedEntity để lưu vết thời điểm gửi thông báo.
/// </summary>
public class ApprovalNotification : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Id của phiếu đề xuất liên quan đến thông báo này.
    /// </summary>
    public Guid RequestId { get; private set; }

    /// <summary>
    /// Tiêu đề của thông báo hiển thị trên chuông thông báo.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Trạng thái mới của đề xuất gắn liền với thông báo (Approved / Rejected).
    /// </summary>
    public RequestStatus Status { get; private set; }

    /// <summary>
    /// Id tài khoản người nhận thông báo (thường là người tạo đề xuất).
    /// </summary>
    public Guid ReceiverId { get; private set; }

    /// <summary>
    /// Id của người đã phê duyệt / từ chối đề xuất (nếu có).
    /// </summary>
    public Guid? ApproverId { get; private set; }

    /// <summary>
    /// Nội dung chi tiết của thông báo gửi tới người dùng.
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Trạng thái đã đọc của thông báo (true: đã đọc, false: chưa đọc).
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// Hàm khởi tạo mặc định cho ORM (EF Core).
    /// </summary>
    protected ApprovalNotification()
    {
    }

    /// <summary>
    /// Khởi tạo một thông báo mới gửi tới người dùng liên quan.
    /// </summary>
    /// <param name="id">Mã khóa chính GUID</param>
    /// <param name="requestId">Id đề xuất liên quan</param>
    /// <param name="title">Tiêu đề thông báo</param>
    /// <param name="status">Trạng thái phê duyệt</param>
    /// <param name="receiverId">Id người nhận thông báo</param>
    /// <param name="approverId">Id người thực hiện duyệt</param>
    /// <param name="message">Nội dung thông báo</param>
    public ApprovalNotification(
        Guid id,
        Guid requestId,
        string title,
        RequestStatus status,
        Guid receiverId,
        Guid? approverId,
        string message) : base(id)
    {
        RequestId = requestId;
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 200);
        Status = status;
        ReceiverId = receiverId;
        ApproverId = approverId;
        Message = Check.NotNullOrWhiteSpace(message, nameof(message), maxLength: 2000);
        IsRead = false;
    }

    /// <summary>
    /// Đánh dấu thông báo là đã đọc.
    /// </summary>
    public void MarkAsRead()
    {
        IsRead = true;
    }
}
