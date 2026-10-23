using System;
using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// DTO tham số cập nhật ngày vào làm việc của nhân viên.
/// </summary>
public class UpdateEmployeeJoinDateDto
{
    [Required]
    public DateTime JoinDate { get; set; }
}
