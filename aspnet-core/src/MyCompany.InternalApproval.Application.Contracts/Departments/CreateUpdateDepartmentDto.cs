using System;
using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Departments;

public class CreateUpdateDepartmentDto
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public Guid? ManagerId { get; set; }
}

