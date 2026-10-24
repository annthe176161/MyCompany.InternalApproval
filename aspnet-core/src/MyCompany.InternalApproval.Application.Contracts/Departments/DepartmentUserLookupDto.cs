using System;

namespace MyCompany.InternalApproval.Departments;

// Thông tin tra cứu nhanh người dùng để phân bổ vào phòng ban
public class DepartmentUserLookupDto
{
    // Id người dùng
    public Guid Id { get; set; }

    // Tên đăng nhập
    public string UserName { get; set; } = string.Empty;

    // Họ và tên đầy đủ
    public string FullName { get; set; } = string.Empty;

    // Địa chỉ email
    public string Email { get; set; } = string.Empty;

    // Tên phòng ban hiện tại (nếu đã thuộc phòng ban)
    public string? CurrentDepartmentName { get; set; }

    // Chức vụ hiện tại (nếu đã thuộc phòng ban)
    public string? CurrentPosition { get; set; }
}

