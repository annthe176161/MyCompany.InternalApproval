using System;
using System.Collections.Generic;

namespace MyCompany.InternalApproval.Employees;

// Thông tin phạm vi quyền truy cập dữ liệu nhân sự của người dùng hiện tại
public class EmployeeAccessScope
{
    // Cấp độ truy cập dữ liệu (SelfOnly, DepartmentManager, Global)
    public EmployeeAccessLevel Level { get; }

    // Cờ kiểm tra quyền toàn cục (Admin hoặc có quyền ViewAll)
    public bool IsGlobal => Level == EmployeeAccessLevel.Global;

    // Cờ kiểm tra quyền trưởng phòng
    public bool IsManager => Level == EmployeeAccessLevel.DepartmentManager;

    // Id của người dùng đang đăng nhập
    public Guid? CurrentUserId { get; }

    // Tập hợp Id các phòng ban mà người dùng hiện tại làm trưởng phòng
    public HashSet<Guid> ManagedDeptIds { get; }

    public EmployeeAccessScope(
        EmployeeAccessLevel level,
        Guid? currentUserId,
        HashSet<Guid>? managedDeptIds = null)
    {
        Level = level;
        CurrentUserId = currentUserId;
        ManagedDeptIds = managedDeptIds ?? new HashSet<Guid>();
    }
}

