using System;
using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Requests;

// Dữ liệu đầu vào khi tạo mới đề xuất
public class CreateRequestDto
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

    // Số tiền (bắt buộc với mua thiết bị và tạm ứng)
    public decimal? Amount { get; set; }

    // Ngày bắt đầu nghỉ phép
    public DateTime? StartDate { get; set; }

    // Ngày kết thúc nghỉ phép
    public DateTime? EndDate { get; set; }
}
