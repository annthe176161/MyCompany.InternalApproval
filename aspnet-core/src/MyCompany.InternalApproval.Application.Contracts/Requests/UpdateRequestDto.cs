using System;
using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Requests;

// Dữ liệu đầu vào khi cập nhật đề xuất ở trạng thái Nháp
public class UpdateRequestDto
{
    // Tiêu đề đề xuất
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    // Nội dung mô tả chi tiết
    [StringLength(4000)]
    public string? Description { get; set; }

    // Loại đề xuất
    [Required]
    public RequestType Type { get; set; }

    // Số tiền đề xuất
    public decimal? Amount { get; set; }

    // Ngày bắt đầu nghỉ phép
    public DateTime? StartDate { get; set; }

    // Ngày kết thúc nghỉ phép
    public DateTime? EndDate { get; set; }
}
