using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MyCompany.InternalApproval.Departments;
using MyCompany.InternalApproval.Requests;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace MyCompany.InternalApproval.Data;

/// <summary>
/// Khởi tạo dữ liệu phân quyền chuẩn (3 vai trò: Admin, Manager, Employee),
/// làm sạch tài khoản cũ, cấp bộ tài khoản mới theo từng phòng ban với 1 mật khẩu chung Company@2026,
/// và tạo dữ liệu đề xuất mẫu thực tế phục vụ kiểm thử.
/// </summary>
public class InternalApprovalDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IdentityUserManager _userManager;
    private readonly IdentityRoleManager _roleManager;
    private readonly IRepository<IdentityRole, Guid> _roleRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IPermissionDataSeeder _permissionDataSeeder;
    private readonly IRepository<Request, Guid> _requestRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<DepartmentMember, Guid> _memberRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ICurrentTenant _currentTenant;

    public ILogger<InternalApprovalDataSeedContributor> Logger { get; set; }

    public const string CommonPassword = "Company@2026";
    public const string RoleAdmin = "admin";
    public const string RoleManager = "Manager";
    public const string RoleEmployee = "Employee";

    public static class RequestPermissions
    {
        public const string Default = "Requests.Default";
        public const string Create = "Requests.Create";
        public const string Edit = "Requests.Edit";
        public const string Delete = "Requests.Delete";
        public const string Approve = "Requests.Approve";
        public const string Reject = "Requests.Reject";
    }

    public static class DepartmentPermissions
    {
        public const string Default = "Departments.Default";
        public const string Create = "Departments.Create";
        public const string Edit = "Departments.Edit";
        public const string Delete = "Departments.Delete";
        public const string ManageMembers = "Departments.ManageMembers";
    }

    public static class EmployeePermissions
    {
        public const string Default = "Employees.Default";
        public const string ViewAll = "Employees.ViewAll";
        public const string Edit = "Employees.Edit";
        public const string Analytics = "Employees.Analytics";
    }

    public InternalApprovalDataSeedContributor(
        IdentityUserManager userManager,
        IdentityRoleManager roleManager,
        IRepository<IdentityRole, Guid> roleRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IPermissionDataSeeder permissionDataSeeder,
        IRepository<Request, Guid> requestRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<DepartmentMember, Guid> memberRepository,
        IGuidGenerator guidGenerator,
        ICurrentTenant currentTenant)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _permissionDataSeeder = permissionDataSeeder;
        _requestRepository = requestRepository;
        _departmentRepository = departmentRepository;
        _memberRepository = memberRepository;
        _guidGenerator = guidGenerator;
        _currentTenant = currentTenant;

        Logger = NullLogger<InternalApprovalDataSeedContributor>.Instance;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        Logger.LogInformation(">>> BẮT ĐẦU SEED DỮ LIỆU PHÂN QUYỀN VÀ TÀI KHOẢN CHUẨN CÔNG TY <<<");

        // 1. Khởi tạo 3 Role chuẩn và phân quyền
        var roles = await SeedRolesAndPermissionsAsync();

        // 2. Làm sạch tài khoản cũ & Khởi tạo bộ tài khoản mới chuẩn
        var users = await SeedUsersAsync(roles);

        // 3. Khởi tạo danh mục phòng ban và phân chia nhân sự vào phòng
        await SeedDepartmentsAndMembersAsync(users);

        // 4. Khởi tạo dữ liệu đề xuất mẫu theo từng nhân viên & người duyệt
        await SeedSampleRequestsAsync(users);

        Logger.LogInformation(">>> HOÀN TẤT SEED DỮ LIỆU PHÂN QUYỀN VÀ TÀI KHOẢN THÀNH CÔNG <<<");
    }

    private async Task<Dictionary<string, IdentityRole>> SeedRolesAndPermissionsAsync()
    {
        var roleDict = new Dictionary<string, IdentityRole>(StringComparer.OrdinalIgnoreCase);

        // 1. Role Admin
        var adminRole = await _roleManager.FindByNameAsync(RoleAdmin);
        if (adminRole == null)
        {
            adminRole = new IdentityRole(_guidGenerator.Create(), RoleAdmin, _currentTenant.Id) { IsStatic = true, IsPublic = true };
            await _roleManager.CreateAsync(adminRole);
        }
        roleDict[RoleAdmin] = adminRole;

        // 2. Role Manager (Quản lý phòng ban)
        var managerRole = await _roleManager.FindByNameAsync(RoleManager);
        if (managerRole == null)
        {
            managerRole = new IdentityRole(_guidGenerator.Create(), RoleManager, _currentTenant.Id) { IsPublic = true };
            await _roleManager.CreateAsync(managerRole);
        }
        roleDict[RoleManager] = managerRole;

        // 3. Role Employee (Nhân viên phòng ban)
        var employeeRole = await _roleManager.FindByNameAsync(RoleEmployee);
        if (employeeRole == null)
        {
            employeeRole = new IdentityRole(_guidGenerator.Create(), RoleEmployee, _currentTenant.Id) { IsPublic = true, IsDefault = true };
            await _roleManager.CreateAsync(employeeRole);
        }
        else if (!employeeRole.IsDefault)
        {
            employeeRole.IsDefault = true;
            await _roleManager.UpdateAsync(employeeRole);
        }
        roleDict[RoleEmployee] = employeeRole;

        // Phân quyền cho Role Admin: Toàn quyền
        var adminPermissions = new[]
        {
            RequestPermissions.Default,
            RequestPermissions.Create,
            RequestPermissions.Edit,
            RequestPermissions.Delete,
            RequestPermissions.Approve,
            RequestPermissions.Reject,
            DepartmentPermissions.Default,
            DepartmentPermissions.Create,
            DepartmentPermissions.Edit,
            DepartmentPermissions.Delete,
            DepartmentPermissions.ManageMembers,
            EmployeePermissions.Default,
            EmployeePermissions.ViewAll,
            EmployeePermissions.Edit,
            EmployeePermissions.Analytics
        };
        await _permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            RoleAdmin,
            adminPermissions,
            _currentTenant.Id
        );

        // Phân quyền cho Role Manager: Xem, Tạo, Sửa, Phê duyệt, Từ chối và Xem phòng ban, Xem và sửa hồ sơ, xem báo cáo phân tích
        var managerPermissions = new[]
        {
            RequestPermissions.Default,
            RequestPermissions.Create,
            RequestPermissions.Edit,
            RequestPermissions.Approve,
            RequestPermissions.Reject,
            DepartmentPermissions.Default,
            EmployeePermissions.Default,
            EmployeePermissions.Edit,
            EmployeePermissions.Analytics
        };
        await _permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            RoleManager,
            managerPermissions,
            _currentTenant.Id
        );

        // Phân quyền cho Role Employee: Xem đề xuất của mình, Tạo mới, Chỉnh sửa, Xóa (khi là nháp), Xem danh bạ/hồ sơ, thống kê cá nhân
        var employeePermissions = new[]
        {
            RequestPermissions.Default,
            RequestPermissions.Create,
            RequestPermissions.Edit,
            RequestPermissions.Delete,
            DepartmentPermissions.Default,
            EmployeePermissions.Default,
            EmployeePermissions.Analytics
        };
        await _permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            RoleEmployee,
            employeePermissions,
            _currentTenant.Id
        );

        return roleDict;
    }

    private async Task<Dictionary<string, IdentityUser>> SeedUsersAsync(Dictionary<string, IdentityRole> roles)
    {
        // Danh sách tài khoản chuẩn theo phòng ban
        var userConfigs = new List<(string UserName, string Name, string Surname, string Email, string Role)>
        {
            // Ban Quản trị
            ("admin", "Quản Trị Viên", "Hệ Thống", "admin@company.com", RoleAdmin),

            // Phòng Công nghệ Thông tin (IT)
            ("manager.it", "Nguyễn Văn Hùng", "Trưởng Phòng IT", "manager.it@company.com", RoleManager),
            ("dev.it", "Lê Minh Tuấn", "Kỹ Sư Phần Mềm", "dev.it@company.com", RoleEmployee),

            // Phòng Hành chính Nhân sự (HR)
            ("manager.hr", "Trần Thị Mai", "Trưởng Phòng Nhân Sự", "manager.hr@company.com", RoleManager),
            ("staff.hr", "Phạm Thu Hà", "Chuyên Viên HR", "staff.hr@company.com", RoleEmployee),

            // Phòng Kế toán Tài chính (Finance)
            ("manager.finance", "Hoàng Đức Thắng", "Kế Toán Trưởng", "manager.finance@company.com", RoleManager),
            ("staff.finance", "Đỗ Quỳnh Anh", "Kế Toán Viên", "staff.finance@company.com", RoleEmployee),

            // Phòng Kinh doanh (Sales)
            ("manager.sales", "Vũ Hải Nam", "Trưởng Phòng Kinh Doanh", "manager.sales@company.com", RoleManager),
            ("staff.sales", "Ngô Bảo Long", "Chuyên Viên Kinh Doanh", "staff.sales@company.com", RoleEmployee),
        };

        var targetUserNames = userConfigs.Select(x => x.UserName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 1. Xóa các tài khoản cũ không nằm trong danh sách chuẩn (dùng repository để tránh NRE của IdentitySecurityLogManager)
        var existingUsers = await _userRepository.GetListAsync(includeDetails: true);
        foreach (var oldUser in existingUsers)
        {
            if (!targetUserNames.Contains(oldUser.UserName))
            {
                Logger.LogInformation("Xóa tài khoản thử nghiệm cũ: {UserName}", oldUser.UserName);
                await _userRepository.DeleteAsync(oldUser, autoSave: true);
            }
        }

        var resultDict = new Dictionary<string, IdentityUser>(StringComparer.OrdinalIgnoreCase);

        // 2. Tạo hoặc cập nhật tài khoản với mật khẩu chung Company@2026
        foreach (var config in userConfigs)
        {
            var user = await _userManager.FindByNameAsync(config.UserName);

            if (user == null)
            {
                user = new IdentityUser(
                    _guidGenerator.Create(),
                    config.UserName,
                    config.Email,
                    _currentTenant.Id
                );
                user.Name = config.Name;
                user.Surname = config.Surname;

                var createResult = await _userManager.CreateAsync(user, CommonPassword);
                if (!createResult.Succeeded)
                {
                    Logger.LogError("Lỗi khi tạo user {UserName}: {Errors}", config.UserName, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }
            }
            else
            {
                user.Name = config.Name;
                user.Surname = config.Surname;
                await _userManager.UpdateAsync(user);

                // Cập nhật lại mật khẩu chuẩn nếu cần
                if (await _userManager.HasPasswordAsync(user))
                {
                    await _userManager.RemovePasswordAsync(user);
                }
                await _userManager.AddPasswordAsync(user, CommonPassword);
            }

            // Gán đúng Role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(config.Role, StringComparer.OrdinalIgnoreCase))
            {
                await _userManager.AddToRoleAsync(user, config.Role);
            }

            resultDict[config.UserName] = user;
        }

        return resultDict;
    }

    private async Task SeedDepartmentsAndMembersAsync(Dictionary<string, IdentityUser> users)
    {
        Logger.LogInformation(">>> BẮT ĐẦU SEED PHÒNG BAN VÀ PHÂN BỔ NHÂN SỰ VÀO VỊ TRÍ <<<");

        var now = DateTime.UtcNow;
        var deptConfigs = new List<(string Code, string Name, string Description, string ManagerUserName, List<(string UserName, string Position, DateTime? JoinDate)> Members)>
        {
            (
                "IT",
                "Công nghệ Thông tin",
                "Phòng Công nghệ và Phát triển Hệ thống phần mềm doanh nghiệp",
                "manager.it",
                new List<(string, string, DateTime?)>
                {
                    ("manager.it", "Trưởng phòng IT", now.AddYears(-3).Date),
                    ("dev.it", "Kỹ sư Phần mềm", now.AddYears(-2).AddMonths(-5).Date)
                }
            ),
            (
                "HR",
                "Hành chính Nhân sự",
                "Phòng Quản lý Nhân sự, Tuyển dụng và Hành chính tổng hợp",
                "manager.hr",
                new List<(string, string, DateTime?)>
                {
                    ("manager.hr", "Trưởng phòng Nhân sự", now.AddYears(-1).Date),
                    ("staff.hr", "Chuyên viên Tuyển dụng & Đào tạo", now.AddMonths(-8).Date)
                }
            ),
            (
                "FINANCE",
                "Kế toán Tài chính",
                "Phòng Kế toán, Quản lý Thu chi và Báo cáo tài chính doanh nghiệp",
                "manager.finance",
                new List<(string, string, DateTime?)>
                {
                    ("manager.finance", "Kế toán trưởng", now.AddYears(-4).Date),
                    ("staff.finance", "Kế toán viên tổng hợp", now.AddDays(-15).Date)
                }
            ),
            (
                "SALES",
                "Kinh doanh & Phát triển Thị trường",
                "Phòng Kinh doanh, Phát triển Khách hàng và Mở rộng thị trường",
                "manager.sales",
                new List<(string, string, DateTime?)>
                {
                    ("manager.sales", "Trưởng phòng Kinh doanh", now.AddYears(-2).Date),
                    ("staff.sales", "Chuyên viên Kinh doanh cao cấp", null) // Fallback về CreationTime
                }
            )
        };

        foreach (var cfg in deptConfigs)
        {
            users.TryGetValue(cfg.ManagerUserName, out var managerUser);

            var dept = await _departmentRepository.FirstOrDefaultAsync(x => x.Code == cfg.Code);
            if (dept == null)
            {
                dept = new Department(
                    _guidGenerator.Create(),
                    cfg.Code,
                    cfg.Name,
                    cfg.Description,
                    managerUser?.Id
                );
                await _departmentRepository.InsertAsync(dept, autoSave: true);
                Logger.LogInformation("Đã khởi tạo phòng ban: {Name} ({Code})", cfg.Name, cfg.Code);
            }
            else
            {
                dept.SetName(cfg.Name);
                dept.SetDescription(cfg.Description);
                dept.SetManager(managerUser?.Id);
                await _departmentRepository.UpdateAsync(dept, autoSave: true);
            }

            foreach (var mem in cfg.Members)
            {
                if (users.TryGetValue(mem.UserName, out var memberUser))
                {
                    var existingMem = await _memberRepository.FirstOrDefaultAsync(x => x.DepartmentId == dept.Id && x.UserId == memberUser.Id);
                    if (existingMem == null)
                    {
                        var newMember = new DepartmentMember(
                            _guidGenerator.Create(),
                            dept.Id,
                            memberUser.Id,
                            mem.Position,
                            mem.JoinDate
                        );
                        await _memberRepository.InsertAsync(newMember, autoSave: true);
                        Logger.LogInformation("Phân bổ nhân sự: {UserName} vào {DeptName} - Vị trí: {Position}", mem.UserName, cfg.Name, mem.Position);
                    }
                    else
                    {
                        existingMem.Position = mem.Position;
                        existingMem.JoinDate = mem.JoinDate;
                        await _memberRepository.UpdateAsync(existingMem, autoSave: true);
                    }
                }
            }
        }
    }

    private async Task SeedSampleRequestsAsync(Dictionary<string, IdentityUser> users)
    {
        // Xóa toàn bộ dữ liệu đề xuất cũ
        var oldRequests = await _requestRepository.GetListAsync();
        if (oldRequests.Any())
        {
            Logger.LogInformation("Xóa {Count} đề xuất cũ để làm sạch dữ liệu.", oldRequests.Count);
            await _requestRepository.DeleteManyAsync(oldRequests, autoSave: true);
        }

        var devIt = users.GetValueOrDefault("dev.it");
        var managerIt = users.GetValueOrDefault("manager.it");
        var staffHr = users.GetValueOrDefault("staff.hr");
        var managerHr = users.GetValueOrDefault("manager.hr");
        var staffFinance = users.GetValueOrDefault("staff.finance");
        var managerFinance = users.GetValueOrDefault("manager.finance");
        var staffSales = users.GetValueOrDefault("staff.sales");
        var managerSales = users.GetValueOrDefault("manager.sales");

        var sampleList = new List<Request>();

        // 1. Hồ sơ của dev.it (IT)
        if (devIt != null)
        {
            // Chờ duyệt
            var r1 = new Request(
                _guidGenerator.Create(),
                "Mua bản quyền phần mềm JetBrains All Products Pack",
                RequestType.Equipment,
                "Gia hạn gói bản quyền phát triển phần mềm thường niên cho đội ngũ kỹ sư IT.",
                15000000m
            );
            r1.CreatorId = devIt.Id;
            r1.CreationTime = DateTime.UtcNow.AddDays(-2);
            r1.Submit();
            r1.AddComment(_guidGenerator.Create(), "Kính gửi anh Hùng phê duyệt ngân sách bản quyền phần mềm.");
            sampleList.Add(r1);

            // Đã duyệt
            var r2 = new Request(
                _guidGenerator.Create(),
                "Nghỉ phép năm 2 ngày giải quyết việc gia đình",
                RequestType.Leave,
                "Tôi xin phép nghỉ 2 ngày từ 25/10 đến 26/10, công việc đã bàn giao cho đồng nghiệp.",
                null,
                DateTime.UtcNow.AddDays(-5).Date,
                DateTime.UtcNow.AddDays(-4).Date
            );
            r2.CreatorId = devIt.Id;
            r2.CreationTime = DateTime.UtcNow.AddDays(-5);
            r2.Submit();
            if (managerIt != null)
            {
                r2.Approve(managerIt.Id);
                r2.AddComment(_guidGenerator.Create(), "[Phê duyệt] Đồng ý cho Tuấn nghỉ phép, bàn giao công việc trước khi nghỉ.");
            }
            sampleList.Add(r2);

            // Bản nháp
            var r3 = new Request(
                _guidGenerator.Create(),
                "Nâng cấp cấu hình RAM 32GB và SSD 1TB cho máy trạm lập trình",
                RequestType.Equipment,
                "Nâng cấp cấu hình máy tính cá nhân để phục vụ build dự án nhanh hơn.",
                6500000m
            );
            r3.CreatorId = devIt.Id;
            r3.CreationTime = DateTime.UtcNow.AddHours(-6);
            sampleList.Add(r3);
        }

        // 2. Hồ sơ của staff.hr (Nhân sự)
        if (staffHr != null)
        {
            // Chờ duyệt
            var r4 = new Request(
                _guidGenerator.Create(),
                "Kinh phí tổ chức chương trình Onboarding nhân viên mới quý 4",
                RequestType.AdvanceMoney,
                "Chi phí mua quà chào đón, văn phòng phẩm và tài liệu đào tạo tân binh.",
                8000000m
            );
            r4.CreatorId = staffHr.Id;
            r4.CreationTime = DateTime.UtcNow.AddDays(-1);
            r4.Submit();
            sampleList.Add(r4);

            // Đã duyệt
            var r5 = new Request(
                _guidGenerator.Create(),
                "Mua sắm thêm máy in văn phòng và mực in dự phòng",
                RequestType.Equipment,
                "Trang bị thêm máy in đa năng cho tầng 3 phục vụ công tác in ấn hợp đồng.",
                12000000m
            );
            r5.CreatorId = staffHr.Id;
            r5.CreationTime = DateTime.UtcNow.AddDays(-8);
            r5.Submit();
            if (managerHr != null)
            {
                r5.Approve(managerHr.Id);
                r5.AddComment(_guidGenerator.Create(), "[Phê duyệt] Đã duyệt, chuyển phòng Tài chính thực hiện giải ngân.");
            }
            sampleList.Add(r5);

            // Đã duyệt (Nghỉ phép)
            var r5b = new Request(
                _guidGenerator.Create(),
                "Nghỉ phép thường niên 3 ngày tái tạo năng lượng",
                RequestType.Leave,
                "Tôi xin nghỉ phép từ 12/08 đến 14/08, công việc tuyển dụng đã bàn giao cho đồng nghiệp.",
                null,
                DateTime.UtcNow.AddDays(-60).Date,
                DateTime.UtcNow.AddDays(-58).Date
            );
            r5b.CreatorId = staffHr.Id;
            r5b.CreationTime = DateTime.UtcNow.AddDays(-65);
            r5b.Submit();
            if (managerHr != null)
            {
                r5b.Approve(managerHr.Id);
                r5b.AddComment(_guidGenerator.Create(), "[Phê duyệt] Đồng ý duyệt 3 ngày nghỉ phép.");
            }
            sampleList.Add(r5b);

            // Bị từ chối
            var r6 = new Request(
                _guidGenerator.Create(),
                "Tổ chức hoạt động Team Building mini cuối tuần",
                RequestType.AdvanceMoney,
                "Chi phí thuê địa điểm và ăn uống gắn kết nội bộ phòng ban.",
                25000000m
            );
            r6.CreatorId = staffHr.Id;
            r6.CreationTime = DateTime.UtcNow.AddDays(-10);
            r6.Submit();
            if (managerHr != null)
            {
                r6.Reject(managerHr.Id, "Ngân sách quý này đã vượt định mức quy định, tạm hoãn sang quý sau.");
                r6.AddComment(_guidGenerator.Create(), "[Từ chối] Ngân sách quý này đã vượt định mức quy định, tạm hoãn sang quý sau.");
            }
            sampleList.Add(r6);
        }

        // 3. Hồ sơ của staff.finance (Kế toán)
        if (staffFinance != null)
        {
            // Chờ duyệt
            var r7 = new Request(
                _guidGenerator.Create(),
                "Mua bản quyền phần mềm kế toán MISA SME 2026",
                RequestType.Equipment,
                "Gói nâng cấp tính năng tự động hóa hóa đơn điện tử và báo cáo tài chính.",
                22500000m
            );
            r7.CreatorId = staffFinance.Id;
            r7.CreationTime = DateTime.UtcNow.AddDays(-3);
            r7.Submit();
            sampleList.Add(r7);

            // Đã duyệt
            var r8 = new Request(
                _guidGenerator.Create(),
                "Tạm ứng công tác kiểm toán chi nhánh Đà Nẵng",
                RequestType.AdvanceMoney,
                "Công tác phí 3 ngày bao gồm vé máy bay, khách sạn và phụ cấp lưu trú.",
                18000000m
            );
            r8.CreatorId = staffFinance.Id;
            r8.CreationTime = DateTime.UtcNow.AddDays(-12);
            r8.Submit();
            if (managerFinance != null)
            {
                r8.Approve(managerFinance.Id);
                r8.AddComment(_guidGenerator.Create(), "[Phê duyệt] Duyệt tạm ứng theo quy chế công tác phí của công ty.");
            }
            sampleList.Add(r8);
        }

        // 4. Hồ sơ của staff.sales (Kinh doanh)
        if (staffSales != null)
        {
            // Chờ duyệt
            var r9 = new Request(
                _guidGenerator.Create(),
                "Chi phí tiếp khách và đàm phán hợp đồng đối tác chiến lược",
                RequestType.AdvanceMoney,
                "Gặp gỡ và ký kết hợp đồng dịch vụ công nghệ cho khách hàng doanh nghiệp lớn.",
                9500000m
            );
            r9.CreatorId = staffSales.Id;
            r9.CreationTime = DateTime.UtcNow.AddHours(-18);
            r9.Submit();
            sampleList.Add(r9);

            // Bản nháp
            var r10 = new Request(
                _guidGenerator.Create(),
                "Mua màn hình trình chiếu di động cho đội ngũ Sales",
                RequestType.Equipment,
                "Trang bị màn hình di động 15.6 inch để demo sản phẩm trực tiếp tại văn phòng khách hàng.",
                7200000m
            );
            r10.CreatorId = staffSales.Id;
            r10.CreationTime = DateTime.UtcNow.AddHours(-2);
            sampleList.Add(r10);

            // Đã duyệt
            var r11 = new Request(
                _guidGenerator.Create(),
                "Nghỉ phép 1 ngày khám sức khỏe định kỳ",
                RequestType.Leave,
                "Xin nghỉ ngày thứ Sáu để đi khám sức khỏe tổng quát theo lịch bệnh viện.",
                null,
                DateTime.UtcNow.AddDays(-15).Date,
                DateTime.UtcNow.AddDays(-15).Date
            );
            r11.CreatorId = staffSales.Id;
            r11.CreationTime = DateTime.UtcNow.AddDays(-15);
            r11.Submit();
            if (managerSales != null)
            {
                r11.Approve(managerSales.Id);
                r11.AddComment(_guidGenerator.Create(), "[Phê duyệt] Đã duyệt.");
            }
            sampleList.Add(r11);
        }

        // Thêm tất cả hồ sơ mẫu vào Database
        await _requestRepository.InsertManyAsync(sampleList, autoSave: true);
        Logger.LogInformation("Đã khởi tạo thành công {Count} hồ sơ đề xuất mẫu chuẩn.", sampleList.Count);
    }
}
