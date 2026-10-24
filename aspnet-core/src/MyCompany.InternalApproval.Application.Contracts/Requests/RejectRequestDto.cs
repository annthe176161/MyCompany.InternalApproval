using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Requests;

// Dữ liệu đầu vào khi từ chối đề xuất
public class RejectRequestDto
{
    // Lý do từ chối (bắt buộc)
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
