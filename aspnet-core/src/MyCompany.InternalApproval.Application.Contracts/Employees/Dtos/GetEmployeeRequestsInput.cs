using Volo.Abp.Application.Dtos;
using MyCompany.InternalApproval.Requests;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// Tham số truy vấn lịch sử đề xuất của nhân viên kèm bộ lọc.
/// </summary>
public class GetEmployeeRequestsInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Bộ lọc theo trạng thái đề xuất: Nháp, Đã gửi duyệt, Đã duyệt, Đã từ chối.
    /// </summary>
    public RequestStatus? Status { get; set; }

    /// <summary>
    /// Bộ lọc theo loại đề xuất: Nghỉ phép, Mua thiết bị, Tạm ứng tiền.
    /// </summary>
    public RequestType? Type { get; set; }

    /// <summary>
    /// Lọc theo năm đề xuất.
    /// </summary>
    public int? Year { get; set; }
}
