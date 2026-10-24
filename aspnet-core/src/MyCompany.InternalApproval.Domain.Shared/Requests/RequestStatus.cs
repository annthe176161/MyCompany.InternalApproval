namespace MyCompany.InternalApproval.Requests;

// Trạng thái của đề xuất nội bộ
public enum RequestStatus
{
    // Bản nháp
    Draft = 0,

    // Đã gửi, đang chờ phê duyệt
    Submitted = 1,

    // Đã được phê duyệt
    Approved = 2,

    // Đã bị từ chối
    Rejected = 3
}
