using System;

namespace MyCompany.InternalApproval.Departments;

// Dữ liệu đầu vào khi cập nhật thông tin thành viên trong phòng ban
public class UpdateDepartmentMemberDto
{
    // Vị trí / chức vụ mới trong phòng ban
    public string Position { get; set; } = string.Empty;

    // Ngày vào làm việc mới
    public DateTime? JoinDate { get; set; }
}

