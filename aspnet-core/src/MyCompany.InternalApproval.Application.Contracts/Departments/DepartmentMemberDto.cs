using System;

namespace MyCompany.InternalApproval.Departments;

// Thông tin thành viên thuộc phòng ban
public class DepartmentMemberDto
{
    // Id bản ghi thành viên phòng ban
    public Guid Id { get; set; }

    // Id phòng ban
    public Guid DepartmentId { get; set; }

    // Id người dùng
    public Guid UserId { get; set; }

    // Tên đăng nhập
    public string UserName { get; set; } = string.Empty;

    // Họ và tên đầy đủ
    public string FullName { get; set; } = string.Empty;

    // Địa chỉ email
    public string Email { get; set; } = string.Empty;

    // Vị trí / chức vụ trong phòng ban
    public string Position { get; set; } = string.Empty;

    // Cờ đánh dấu có phải trưởng phòng hay không
    public bool IsManager { get; set; }

    // Ngày vào làm được cấu hình
    public DateTime? JoinDate { get; set; }

    // Ngày vào làm hiệu lực
    public DateTime EffectiveJoinDate { get; set; }

    // Chuỗi thâm niên định dạng tiếng Việt
    public string? TenureFormatted { get; set; }

    // Thời điểm thêm vào phòng ban
    public DateTime CreationTime { get; set; }
}
