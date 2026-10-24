using System;

namespace MyCompany.InternalApproval.Departments;

// Dữ liệu đầu vào khi thêm nhân viên vào phòng ban
public class AddDepartmentMemberDto
{
    // Id người dùng cần thêm vào phòng ban
    public Guid UserId { get; set; }

    // Vị trí / chức vụ trong phòng ban
    public string Position { get; set; } = string.Empty;

    // Ngày vào làm việc
    public DateTime? JoinDate { get; set; }
}

