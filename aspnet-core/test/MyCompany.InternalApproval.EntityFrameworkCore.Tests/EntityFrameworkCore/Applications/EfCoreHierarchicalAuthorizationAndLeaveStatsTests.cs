using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyCompany.InternalApproval.Departments;
using MyCompany.InternalApproval.Employees;
using MyCompany.InternalApproval.Permissions;
using MyCompany.InternalApproval.Requests;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;
using Xunit;

namespace MyCompany.InternalApproval.EntityFrameworkCore.Applications;

[Collection(InternalApprovalTestConsts.CollectionDefinitionName)]
public class EfCoreHierarchicalAuthorizationAndLeaveStatsTests
    : InternalApprovalApplicationTestBase<InternalApprovalEntityFrameworkCoreTestModule>
{
    private readonly IEmployeeAppService _employeeAppService;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<DepartmentMember, Guid> _memberRepository;
    private readonly IRepository<Request, Guid> _requestRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;
    private readonly IPermissionManager _permissionManager;

    public EfCoreHierarchicalAuthorizationAndLeaveStatsTests()
    {
        _employeeAppService = GetRequiredService<IEmployeeAppService>();
        _userRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        _departmentRepository = GetRequiredService<IRepository<Department, Guid>>();
        _memberRepository = GetRequiredService<IRepository<DepartmentMember, Guid>>();
        _requestRepository = GetRequiredService<IRepository<Request, Guid>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        _permissionManager = GetRequiredService<IPermissionManager>();
    }

    private IDisposable LoginAs(Guid userId, string userName, string role = "admin")
    {
        var claims = new[]
        {
            new Claim(AbpClaimTypes.UserId, userId.ToString()),
            new Claim(AbpClaimTypes.UserName, userName),
            new Claim(AbpClaimTypes.Role, role),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test", AbpClaimTypes.UserName, AbpClaimTypes.Role);
        return _currentPrincipalAccessor.Change(new ClaimsPrincipal(identity));
    }

    // =========================================================================
    // 1. ADMIN & VIEWALL PERMISSION TESTS
    // =========================================================================

    [Fact]
    public async Task Admin_Can_Access_All_Departments_And_Employees()
    {
        var admin = (await _userRepository.GetListAsync()).First(u => u.UserName == "admin");
        var devIt = (await _userRepository.GetListAsync()).First(u => u.UserName == "dev.it");
        var staffSales = (await _userRepository.GetListAsync()).First(u => u.UserName == "staff.sales");
        var staffHr = (await _userRepository.GetListAsync()).First(u => u.UserName == "staff.hr");

        using (LoginAs(admin.Id, "admin", "admin"))
        {
            var list = await _employeeAppService.GetListAsync(new GetEmployeeListInput { MaxResultCount = 50 });
            list.TotalCount.ShouldBeGreaterThanOrEqualTo(8);

            var itProfile = await _employeeAppService.GetAsync(devIt.Id);
            itProfile.ShouldNotBeNull();
            itProfile.UserName.ShouldBe("dev.it");

            var salesProfile = await _employeeAppService.GetAsync(staffSales.Id);
            salesProfile.ShouldNotBeNull();
            salesProfile.UserName.ShouldBe("staff.sales");

            var hrProfile = await _employeeAppService.GetAsync(staffHr.Id);
            hrProfile.ShouldNotBeNull();
            hrProfile.UserName.ShouldBe("staff.hr");

            var leaveReport = await _employeeAppService.GetLeaveReportAsync(new GetLeaveReportInput { MaxResultCount = 50 });
            leaveReport.TotalCount.ShouldBeGreaterThanOrEqualTo(8);
        }
    }

    [Fact]
    public async Task User_With_ViewAll_Permission_Can_Access_All_Employees()
    {
        var users = await _userRepository.GetListAsync();
        var staffFinance = users.First(u => u.UserName == "staff.finance");
        var devIt = users.First(u => u.UserName == "dev.it");

        // Cấp quyền Employees.ViewAll trực tiếp cho user staff.finance
        await _permissionManager.SetForUserAsync(staffFinance.Id, InternalApprovalPermissions.Employees.ViewAll, true);

        // staff.finance đăng nhập với role Employee nhưng có quyền ViewAll
        using (LoginAs(staffFinance.Id, "staff.finance", "Employee"))
        {
            var list = await _employeeAppService.GetListAsync(new GetEmployeeListInput { MaxResultCount = 50 });
            list.TotalCount.ShouldBeGreaterThanOrEqualTo(8);

            var itProfile = await _employeeAppService.GetAsync(devIt.Id);
            itProfile.ShouldNotBeNull();
            itProfile.UserName.ShouldBe("dev.it");
        }

        // Dọn dẹp permission
        await _permissionManager.SetForUserAsync(staffFinance.Id, InternalApprovalPermissions.Employees.ViewAll, false);
    }

    // =========================================================================
    // 2. DEPARTMENT MANAGER SCOPED ACCESS & NEGATIVE TESTS
    // =========================================================================

    [Fact]
    public async Task Manager_Can_Only_List_Employees_In_Managed_Department()
    {
        var managerIt = (await _userRepository.GetListAsync()).First(u => u.UserName == "manager.it");
        var itDept = (await _departmentRepository.GetListAsync()).First(d => d.Code == "IT");

        using (LoginAs(managerIt.Id, "manager.it", "Manager"))
        {
            var result = await _employeeAppService.GetListAsync(new GetEmployeeListInput { MaxResultCount = 50 });

            // IT chỉ gồm manager.it và dev.it
            result.Items.ShouldAllBe(x => x.DepartmentId == itDept.Id || x.Id == managerIt.Id);
            result.Items.ShouldNotContain(x => x.UserName == "staff.sales");
            result.Items.ShouldNotContain(x => x.UserName == "staff.hr");
            result.Items.ShouldNotContain(x => x.UserName == "staff.finance");

            // Có thể xem chi tiết hồ sơ thành viên phòng IT
            var devIt = (await _userRepository.GetListAsync()).First(u => u.UserName == "dev.it");
            var devProfile = await _employeeAppService.GetAsync(devIt.Id);
            devProfile.ShouldNotBeNull();
            devProfile.UserName.ShouldBe("dev.it");
        }
    }

    [Fact]
    public async Task Manager_Accessing_CrossDepartment_Employee_Throws_AbpAuthorizationException()
    {
        var managerIt = (await _userRepository.GetListAsync()).First(u => u.UserName == "manager.it");
        var staffHr = (await _userRepository.GetListAsync()).First(u => u.UserName == "staff.hr");
        var staffSales = (await _userRepository.GetListAsync()).First(u => u.UserName == "staff.sales");

        using (LoginAs(managerIt.Id, "manager.it", "Manager"))
        {
            // 1. Xem chi tiết hồ sơ nhân sự phòng khác -> Chặn
            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await _employeeAppService.GetAsync(staffHr.Id);
            });

            // 2. Xem lịch sử đề xuất của nhân sự phòng khác -> Chặn
            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await _employeeAppService.GetRequestsAsync(staffSales.Id, new GetEmployeeRequestsInput());
            });

            // 3. Xem timeline nghỉ phép của nhân sự phòng khác -> Chặn
            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await _employeeAppService.GetLeaveTimelineAsync(staffHr.Id);
            });

            // 4. Xem thống kê theo năm của nhân sự phòng khác -> Chặn
            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await _employeeAppService.GetYearlyStatsAsync(staffSales.Id, 2026);
            });

            // 5. Thử cập nhật ngày vào làm của nhân sự phòng khác -> Chặn
            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await _employeeAppService.UpdateJoinDateAsync(staffHr.Id, new UpdateEmployeeJoinDateDto
                {
                    JoinDate = DateTime.UtcNow.AddYears(-1)
                });
            });
        }
    }

    [Fact]
    public async Task Manager_Filtering_By_Unmanaged_Department_Throws_AbpAuthorizationException()
    {
        var managerIt = (await _userRepository.GetListAsync()).First(u => u.UserName == "manager.it");
        var hrDept = (await _departmentRepository.GetListAsync()).First(d => d.Code == "HR");

        using (LoginAs(managerIt.Id, "manager.it", "Manager"))
        {
            // GetListAsync lọc theo phòng ban không thuộc quyền quản lý -> Ném ngoại lệ
            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await _employeeAppService.GetListAsync(new GetEmployeeListInput
                {
                    DepartmentId = hrDept.Id
                });
            });

            // GetLeaveReportAsync lọc theo phòng ban không thuộc quyền quản lý -> Ném ngoại lệ
            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await _employeeAppService.GetLeaveReportAsync(new GetLeaveReportInput
                {
                    DepartmentId = hrDept.Id
                });
            });
        }
    }

    // =========================================================================
    // 3. REGULAR EMPLOYEE STRICT SELF-ACCESS TESTS
    // =========================================================================

    [Fact]
    public async Task Regular_Employee_Can_Only_View_Own_Profile_And_Blocked_From_Others()
    {
        var devIt = (await _userRepository.GetListAsync()).First(u => u.UserName == "dev.it");
        var managerIt = (await _userRepository.GetListAsync()).First(u => u.UserName == "manager.it");
        var staffHr = (await _userRepository.GetListAsync()).First(u => u.UserName == "staff.hr");

        using (LoginAs(devIt.Id, "dev.it", "Employee"))
        {
            // 1. dev.it xem chính mình -> Thành công
            var self = await _employeeAppService.GetAsync(devIt.Id);
            self.ShouldNotBeNull();
            self.UserName.ShouldBe("dev.it");

            // 2. dev.it xem trưởng phòng của mình -> Bị chặn
            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await _employeeAppService.GetAsync(managerIt.Id);
            });

            // 3. dev.it xem đồng nghiệp phòng khác -> Bị chặn
            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await _employeeAppService.GetAsync(staffHr.Id);
            });

            // 4. dev.it xem danh sách nhân sự -> Chỉ trả về bản thân
            var list = await _employeeAppService.GetListAsync(new GetEmployeeListInput());
            list.TotalCount.ShouldBe(1);
            list.Items.First().Id.ShouldBe(devIt.Id);

            // 5. dev.it xem báo cáo ngày nghỉ phép -> Chỉ thấy bản thân
            var report = await _employeeAppService.GetLeaveReportAsync(new GetLeaveReportInput());
            report.TotalCount.ShouldBe(1);
            report.Items.First().Id.ShouldBe(devIt.Id);
        }
    }

    // =========================================================================
    // 4. LEAVE STATISTICS CALCULATION & REPORT INVARIANTS
    // =========================================================================

    [Fact]
    public async Task LeaveReport_Calculates_Approved_Leave_Days_Accurately()
    {
        var admin = (await _userRepository.GetListAsync()).First(u => u.UserName == "admin");
        var currentYear = DateTime.UtcNow.Year;

        using (LoginAs(admin.Id, "admin", "admin"))
        {
            var report = await _employeeAppService.GetLeaveReportAsync(new GetLeaveReportInput
            {
                Year = currentYear,
                MaxResultCount = 50
            });

            report.ShouldNotBeNull();
            report.TotalCount.ShouldBeGreaterThan(0);

            // dev.it có 1 đơn nghỉ phép Approved 2 ngày (25/10 - 26/10) trong seeder
            var devReport = report.Items.FirstOrDefault(x => x.UserName == "dev.it");
            devReport.ShouldNotBeNull();
            devReport.DepartmentCode.ShouldBe("IT");
            devReport.TotalApprovedLeaveDays.ShouldBe(2.0);
            devReport.TotalApprovedLeaveRequests.ShouldBe(1);
            devReport.TotalSentLeaveRequests.ShouldBeGreaterThanOrEqualTo(1);
            devReport.LatestLeaveStartDate.ShouldNotBeNull();
            devReport.LatestLeavePeriodFormatted.ShouldNotBeNull();
            devReport.LatestLeavePeriodFormatted!.ShouldContain("2 ngày");

            // staff.sales có 1 đơn nghỉ phép Approved 1 ngày (15/10 - 15/10) trong seeder
            var salesReport = report.Items.FirstOrDefault(x => x.UserName == "staff.sales");
            salesReport.ShouldNotBeNull();
            salesReport.TotalApprovedLeaveDays.ShouldBe(1.0);
            salesReport.TotalApprovedLeaveRequests.ShouldBe(1);
            salesReport.LatestLeavePeriodFormatted!.ShouldContain("1 ngày");
        }
    }

    [Fact]
    public async Task LeaveReport_Zero_Leave_Employee_Returns_Zero_And_ChuaCo()
    {
        var admin = (await _userRepository.GetListAsync()).First(u => u.UserName == "admin");

        // Tạo 1 tài khoản nhân viên mới không có đề xuất nào
        var newUser = new IdentityUser(Guid.NewGuid(), "zero.leave.user", "zero.leave@company.com");
        await _userRepository.InsertAsync(newUser, autoSave: true);

        using (LoginAs(admin.Id, "admin", "admin"))
        {
            var report = await _employeeAppService.GetLeaveReportAsync(new GetLeaveReportInput
            {
                Filter = "zero.leave.user"
            });

            report.TotalCount.ShouldBe(1);
            var item = report.Items.First();
            item.TotalApprovedLeaveDays.ShouldBe(0);
            item.TotalSentLeaveRequests.ShouldBe(0);
            item.TotalApprovedLeaveRequests.ShouldBe(0);
            item.LatestLeaveStartDate.ShouldBeNull();
            item.LatestLeaveEndDate.ShouldBeNull();
            item.LatestLeaveDays.ShouldBeNull();
            item.LatestLeavePeriodFormatted.ShouldBe("Chưa có");
        }
    }
}
