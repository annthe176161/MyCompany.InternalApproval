using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Requests;

public class CreateRequestCommentDto
{
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}

