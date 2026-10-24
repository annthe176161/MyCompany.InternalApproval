using MyCompany.InternalApproval.Requests;

namespace MyCompany.InternalApproval.Employees;

// Số liệu thống kê chi tiết theo từng loại đề xuất
public class RequestTypeStatDto
{
    // Loại đề xuất
    public RequestType Type { get; set; }

    // Tên hiển thị của loại đề xuất
    public string TypeName { get; set; } = string.Empty;

    // Số lượng đề xuất thuộc loại này
    public int Count { get; set; }

    // Tổng số tiền của các đề xuất thuộc loại này
    public decimal TotalAmount { get; set; }
}

