using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Requests;

public class ApproveRequestDto
{
    [StringLength(1000)]
    public string? Note { get; set; }
}

