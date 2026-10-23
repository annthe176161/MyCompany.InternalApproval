using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Requests;

public class RejectRequestDto
{
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

