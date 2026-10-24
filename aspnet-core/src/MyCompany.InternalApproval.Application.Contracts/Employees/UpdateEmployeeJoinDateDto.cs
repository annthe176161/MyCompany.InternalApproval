using System;
using System.ComponentModel.DataAnnotations;

namespace MyCompany.InternalApproval.Employees;

// Tham số cập nhật ngày vào làm việc của nhân viên
public class UpdateEmployeeJoinDateDto
{
    // Ngày bắt đầu làm việc mới
    [Required]
    public DateTime JoinDate { get; set; }
}

