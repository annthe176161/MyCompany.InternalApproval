namespace MyCompany.InternalApproval;

// Mã lỗi nghiệp vụ dùng chung trong toàn hệ thống
public static class InternalApprovalDomainErrorCodes
{
    // Chỉ đề xuất ở trạng thái Nháp mới được gửi duyệt
    public const string OnlyDraftCanBeSubmitted = "InternalApproval:00001";

    // Chỉ người tạo mới được gửi duyệt đề xuất
    public const string OnlyCreatorCanSubmit = "InternalApproval:00002";

    // Không thể tự phê duyệt đề xuất của chính mình
    public const string CannotApproveOwnRequest = "InternalApproval:00003";

    // Chỉ đề xuất đang chờ duyệt mới được phê duyệt
    public const string OnlySubmittedCanBeApproved = "InternalApproval:00004";

    // Bắt buộc nhập lý do khi từ chối đề xuất
    public const string RejectionReasonRequired = "InternalApproval:00005";

    // Chỉ đề xuất đang chờ duyệt mới được từ chối
    public const string OnlySubmittedCanBeRejected = "InternalApproval:00006";

    // Bắt buộc nhập số tiền hợp lệ lớn hơn 0 đối với loại đề xuất này
    public const string AmountRequiredForType = "InternalApproval:00007";

    // Chỉ đề xuất ở trạng thái Nháp mới được chỉnh sửa
    public const string OnlyDraftCanBeEdited = "InternalApproval:00008";

    // Chỉ người tạo mới được chỉnh sửa đề xuất
    public const string OnlyCreatorCanEdit = "InternalApproval:00009";

    // Chỉ đề xuất ở trạng thái Nháp mới được xóa
    public const string OnlyDraftCanBeDeleted = "InternalApproval:00010";

    // Chỉ người tạo mới được xóa đề xuất
    public const string OnlyCreatorCanDelete = "InternalApproval:00011";

    // Lỗi ánh xạ dữ liệu AutoMapper
    public const string MappingError = "InternalApproval:00012";

    // Ngày kết thúc không được nhỏ hơn ngày bắt đầu
    public const string InvalidDateRange = "InternalApproval:00013";

    // Ngày vào làm việc không hợp lệ
    public const string InvalidJoinDate = "InternalApproval:00014";

    // Mã phòng ban đã tồn tại
    public const string DepartmentCodeAlreadyExists = "InternalApproval:00015";

    // Nhân viên không tồn tại trong phòng ban
    public const string DepartmentMemberNotFound = "InternalApproval:00016";
}
