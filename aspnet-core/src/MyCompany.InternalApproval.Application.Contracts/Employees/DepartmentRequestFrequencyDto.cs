using System;

namespace MyCompany.InternalApproval.Employees;

// Thống kê tần suất gửi đề xuất theo phòng ban
public class DepartmentRequestFrequencyDto
{
    // Id phòng ban
    public Guid DepartmentId { get; set; }

    // Mã phòng ban
    public string DepartmentCode { get; set; } = string.Empty;

    // Tên phòng ban
    public string DepartmentName { get; set; } = string.Empty;

    // Tổng số đề xuất của phòng ban trong năm
    public int TotalRequests { get; set; }

    // Số đề xuất trung bình trên mỗi nhân viên của phòng ban
    public double RequestsPerEmployee { get; set; }
}

