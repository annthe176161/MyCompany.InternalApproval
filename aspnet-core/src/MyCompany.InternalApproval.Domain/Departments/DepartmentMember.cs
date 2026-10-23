using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace MyCompany.InternalApproval.Departments;

/// <summary>
/// Thực thể Thành viên Phòng ban (DepartmentMember Entity).
/// Liên kết người dùng (IdentityUser) vào một phòng ban cụ thể kèm chức danh công tác và ngày vào công ty.
/// Kế thừa CreationAuditedEntity để lưu vết thời gian và người thêm thành viên.
/// </summary>
public class DepartmentMember : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Id của phòng ban mà nhân viên đang trực thuộc.
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Id tài khoản người dùng (AbpUsers / IdentityUser).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Vị trí / chức danh trong phòng ban (ví dụ: Trưởng phòng, Phó phòng, Chuyên viên, Lập trình viên).
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Ngày chính thức gia nhập công ty (dùng để tính thâm niên và số ngày phép năm).
    /// </summary>
    public DateTime? JoinDate { get; set; }

    /// <summary>
    /// Hàm khởi tạo mặc định cho ORM (EF Core).
    /// </summary>
    protected DepartmentMember()
    {
    }

    /// <summary>
    /// Khởi tạo thành viên phòng ban với đầy đủ thông tin vị trí và ngày gia nhập.
    /// </summary>
    /// <param name="id">Mã khóa chính GUID</param>
    /// <param name="departmentId">Id phòng ban trực thuộc</param>
    /// <param name="userId">Id tài khoản nhân viên</param>
    /// <param name="position">Chức danh công tác</param>
    /// <param name="joinDate">Ngày gia nhập công ty (nếu có)</param>
    public DepartmentMember(Guid id, Guid departmentId, Guid userId, string position, DateTime? joinDate = null) : base(id)
    {
        DepartmentId = departmentId;
        UserId = userId;
        Position = position;
        JoinDate = joinDate;
    }
}
