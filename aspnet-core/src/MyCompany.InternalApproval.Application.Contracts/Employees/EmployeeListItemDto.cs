using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Employees;

// Thông tin nhân viên hiển thị trong bảng danh sách và tìm kiếm nhanh
public class EmployeeListItemDto : EntityDto<Guid>
{
    // Tên đăng nhập
    public string UserName { get; set; } = string.Empty;

    // Họ và tên đầy đủ
    public string FullName { get; set; } = string.Empty;

    // Địa chỉ email
    public string Email { get; set; } = string.Empty;

    // Số điện thoại
    public string? PhoneNumber { get; set; }

    // Id phòng ban hiện tại
    public Guid? DepartmentId { get; set; }

    // Mã phòng ban hiện tại
    public string? DepartmentCode { get; set; }

    // Tên phòng ban hiện tại
    public string? DepartmentName { get; set; }

    // Chức vụ / vị trí trong phòng ban
    public string Position { get; set; } = string.Empty;

    // Cờ đánh dấu có phải trưởng phòng hay không
    public bool IsManager { get; set; }

    // Ngày bắt đầu làm việc hiệu lực (nếu chưa cấu hình thì lấy CreationTime)
    public DateTime JoinDate { get; set; }

    // Ngày vào làm được lưu trong bản ghi thành viên phòng ban (null nếu chưa thiết lập)
    public DateTime? RawJoinDate { get; set; }

    // Thời điểm tạo tài khoản người dùng trên hệ thống
    public DateTime CreationTime { get; set; }

    // Thâm niên làm việc tính theo tổng số tháng
    public int TenureMonths { get; set; }

    // Thâm niên làm việc định dạng tiếng Việt ("2 năm 5 tháng", "8 tháng", "< 1 tháng")
    public string TenureFormatted { get; set; } = string.Empty;

    // Tổng số lượng đề xuất nhân viên gửi trong năm hiện tại
    public int CurrentYearRequestCount { get; set; }
}

