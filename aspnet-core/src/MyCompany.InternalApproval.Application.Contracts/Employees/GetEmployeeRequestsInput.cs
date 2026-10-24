using MyCompany.InternalApproval.Requests;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Employees;

// Tham số truy vấn lịch sử đề xuất của nhân viên kèm bộ lọc
public class GetEmployeeRequestsInput : PagedAndSortedResultRequestDto
{
    // Bộ lọc theo trạng thái đề xuất
    public RequestStatus? Status { get; set; }

    // Bộ lọc theo loại đề xuất
    public RequestType? Type { get; set; }

    // Bộ lọc theo năm đề xuất
    public int? Year { get; set; }
}

