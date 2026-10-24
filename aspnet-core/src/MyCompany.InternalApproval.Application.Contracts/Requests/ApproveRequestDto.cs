using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Requests;

// Dữ liệu đầu vào khi phê duyệt đề xuất
public class ApproveRequestDto
{
    // Ghi chú phê duyệt (tùy chọn)
    [StringLength(1000)]
    public string? Note { get; set; }
}
