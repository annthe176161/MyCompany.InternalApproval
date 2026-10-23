using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Requests;

/// <summary>
/// Thực thể Bình luận / Trao đổi trong Đề xuất (RequestComment Entity).
/// Lưu lại các phản hồi, trao đổi giữa người tạo và người phê duyệt trong quá trình xử lý đề xuất.
/// Kế thừa CreationAuditedEntity để tự động ghi nhận thời gian và người gửi bình luận.
/// </summary>
public class RequestComment : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Id của phiếu đề xuất chứa bình luận này.
    /// </summary>
    public Guid RequestId { get; private set; }

    /// <summary>
    /// Nội dung bình luận, trao đổi ý kiến.
    /// </summary>
    public string Content { get; private set; } = null!;

    /// <summary>
    /// Hàm khởi tạo mặc định cho ORM (EF Core).
    /// </summary>
    protected RequestComment()
    {
        Content = null!;
    }

    /// <summary>
    /// Khởi tạo một bình luận mới thuộc về một đề xuất.
    /// </summary>
    /// <param name="id">Mã khóa chính GUID</param>
    /// <param name="requestId">Id đề xuất liên quan</param>
    /// <param name="content">Nội dung bình luận</param>
    public RequestComment(Guid id, Guid requestId, string content) : base(id)
    {
        RequestId = requestId;
        SetContent(content);
    }

    /// <summary>
    /// Cập nhật nội dung bình luận với ràng buộc không rỗng và tối đa 2000 ký tự.
    /// </summary>
    public void SetContent(string content)
    {
        Content = Check.NotNullOrWhiteSpace(content, nameof(content), maxLength: 2000);
    }
}
