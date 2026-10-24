using System;

namespace MyCompany.InternalApproval.Employees;

// Thống kê thâm niên làm việc theo phòng ban
public class DepartmentTenureDto
{
    // Id phòng ban
    public Guid DepartmentId { get; set; }

    // Mã phòng ban
    public string DepartmentCode { get; set; } = string.Empty;

    // Tên phòng ban
    public string DepartmentName { get; set; } = string.Empty;

    // Số tháng thâm niên trung bình của phòng ban
    public double AverageTenureMonths { get; set; }

    // Chuỗi thâm niên trung bình định dạng tiếng Việt
    public string AverageTenureFormatted { get; set; } = string.Empty;

    // Số tháng thâm niên thấp nhất trong phòng ban
    public int MinTenureMonths { get; set; }

    // Số tháng thâm niên cao nhất trong phòng ban
    public int MaxTenureMonths { get; set; }
}

