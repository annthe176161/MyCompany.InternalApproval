using System;

namespace MyCompany.InternalApproval.Employees;

// Thông tin nhân viên có số lượng đề xuất nhiều nhất trong năm
public class TopRequesterDto
{
    // Id người dùng
    public Guid UserId { get; set; }

    // Họ và tên nhân viên
    public string FullName { get; set; } = string.Empty;

    // Tên phòng ban
    public string DepartmentName { get; set; } = string.Empty;

    // Tổng số lượng đề xuất đã tạo
    public int RequestCount { get; set; }
}

