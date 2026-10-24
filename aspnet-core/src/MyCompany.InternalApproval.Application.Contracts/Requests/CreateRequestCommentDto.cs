using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Requests;

// Dữ liệu đầu vào khi thêm bình luận vào đề xuất
public class CreateRequestCommentDto
{
    // Nội dung bình luận
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}
