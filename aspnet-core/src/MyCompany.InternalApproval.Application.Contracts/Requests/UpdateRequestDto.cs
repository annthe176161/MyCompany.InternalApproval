using System;
using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Requests;

public class UpdateRequestDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required]
    public RequestType Type { get; set; }

    public decimal? Amount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

