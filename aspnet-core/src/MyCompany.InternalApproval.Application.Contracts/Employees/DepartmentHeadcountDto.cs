using System;

namespace MyCompany.InternalApproval.Employees;

// Thống kê số lượng và tỷ lệ nhân sự theo phòng ban
public class DepartmentHeadcountDto
{
    // Id phòng ban
    public Guid DepartmentId { get; set; }

    // Mã phòng ban
    public string DepartmentCode { get; set; } = string.Empty;

    // Tên phòng ban
    public string DepartmentName { get; set; } = string.Empty;

    // Số lượng nhân sự thuộc phòng ban
    public int Headcount { get; set; }

    // Tỷ lệ phần trăm nhân sự so với toàn công ty
    public double Percentage { get; set; }
}

