namespace MyCompany.InternalApproval.Employees;

// Phạm vi quyền truy cập dữ liệu nhân sự
public enum EmployeeAccessLevel
{
    // Chỉ được phép xem hồ sơ của chính mình
    SelfOnly = 0,

    // Trưởng phòng được xem nhân sự trong phòng ban mình quản lý
    DepartmentManager = 1,

    // Quản trị viên hoặc người có quyền ViewAll được xem toàn công ty
    Global = 2
}

