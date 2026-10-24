using System;
using MyCompany.InternalApproval.Requests;

namespace MyCompany.InternalApproval.Employees;

// Thông tin một đợt nghỉ phép phục vụ hiển thị biểu đồ / timeline nghỉ phép
public class LeaveTimelineItemDto
{
    // Id đề xuất nghỉ phép
    public Guid RequestId { get; set; }

    // Tiêu đề đề xuất
    public string Title { get; set; } = string.Empty;

    // Ngày bắt đầu nghỉ
    public DateTime StartDate { get; set; }

    // Ngày kết thúc nghỉ
    public DateTime EndDate { get; set; }

    // Số ngày nghỉ
    public double Days { get; set; }

    // Lý do xin nghỉ
    public string Reason { get; set; } = string.Empty;

    // Trạng thái phê duyệt
    public RequestStatus Status { get; set; }

    // Tên hiển thị trạng thái
    public string StatusName { get; set; } = string.Empty;

    // Thời gian tạo đề xuất
    public DateTime CreationTime { get; set; }

    // Họ tên người phê duyệt
    public string? ApproverName { get; set; }

    // Thời điểm phê duyệt
    public DateTime? ApprovalTime { get; set; }

    // Lý do từ chối (nếu bị từ chối)
    public string? RejectionReason { get; set; }
}

