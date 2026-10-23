using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyCompany.InternalApproval.Departments;
using MyCompany.InternalApproval.Employees;
using MyCompany.InternalApproval.Requests;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Xunit;

namespace MyCompany.InternalApproval.Employees;

public abstract class EmployeeAppService_Tests<TStartupModule> : InternalApprovalApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly IEmployeeAppService EmployeeAppService;
    protected readonly IRepository<IdentityUser, Guid> UserRepository;
    protected readonly IRepository<Department, Guid> DepartmentRepository;
    protected readonly IRepository<DepartmentMember, Guid> MemberRepository;
    protected readonly IRepository<Request, Guid> RequestRepository;
    protected readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;

    protected EmployeeAppService_Tests()
    {
        EmployeeAppService = GetRequiredService<IEmployeeAppService>();
        UserRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        DepartmentRepository = GetRequiredService<IRepository<Department, Guid>>();
        MemberRepository = GetRequiredService<IRepository<DepartmentMember, Guid>>();
        RequestRepository = GetRequiredService<IRepository<Request, Guid>>();
        CurrentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    protected IDisposable LoginAs(Guid userId, string userName, string role = "admin")
    {
        var claims = new[]
        {
            new Claim(AbpClaimTypes.UserId, userId.ToString()),
            new Claim(AbpClaimTypes.UserName, userName),
            new Claim(AbpClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        return CurrentPrincipalAccessor.Change(new ClaimsPrincipal(identity));
    }

    [Fact]
    public async Task Should_Get_Employee_List_With_Pagination_And_Filter()
    {
        var adminUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "admin");
        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            // Act: Lấy danh sách toàn bộ nhân viên
            var result = await EmployeeAppService.GetListAsync(new GetEmployeeListInput
            {
                MaxResultCount = 20
            });

            // Assert
            result.ShouldNotBeNull();
            result.TotalCount.ShouldBeGreaterThan(0);
            result.Items.Count.ShouldBeGreaterThan(0);

            // Tìm kiếm theo từ khóa "dev.it"
            var devResult = await EmployeeAppService.GetListAsync(new GetEmployeeListInput
            {
                Filter = "dev.it"
            });
            devResult.Items.ShouldContain(x => x.UserName == "dev.it");

            // Lọc theo phòng ban IT
            var itDept = (await DepartmentRepository.GetListAsync()).First(x => x.Code == "IT");
            var itResult = await EmployeeAppService.GetListAsync(new GetEmployeeListInput
            {
                DepartmentId = itDept.Id
            });
            itResult.Items.ShouldAllBe(x => x.DepartmentId == itDept.Id);
        }
    }

    [Fact]
    public async Task Should_Calculate_Tenure_And_Fallback_To_CreationTime_When_JoinDate_Is_Null()
    {
        var adminUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "admin");
        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            // staff.sales được cấu hình JoinDate = null trong seeder
            var salesUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "staff.sales");

            var profile = await EmployeeAppService.GetAsync(salesUser.Id);

            profile.ShouldNotBeNull();
            profile.RawJoinDate.ShouldBeNull();
            profile.JoinDate.Date.ShouldBe(salesUser.CreationTime.Date);
            profile.TenureFormatted.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Should_Calculate_Tenure_Format_Correctly_In_Vietnamese()
    {
        var baseDate = new DateTime(2026, 10, 22);

        // 2 năm 5 tháng
        var t1 = TenureCalculator.Calculate(new DateTime(2024, 5, 15), baseDate);
        t1.Years.ShouldBe(2);
        t1.Months.ShouldBe(5);
        TenureCalculator.FormatVietnamese(t1.Years, t1.Months).ShouldBe("2 năm 5 tháng");

        // 8 tháng
        var t2 = TenureCalculator.Calculate(new DateTime(2026, 2, 15), baseDate);
        t2.Years.ShouldBe(0);
        t2.Months.ShouldBe(8);
        TenureCalculator.FormatVietnamese(t2.Years, t2.Months).ShouldBe("8 tháng");

        // < 1 tháng
        var t3 = TenureCalculator.Calculate(new DateTime(2026, 10, 5), baseDate);
        t3.Years.ShouldBe(0);
        t3.Months.ShouldBe(0);
        TenureCalculator.FormatVietnamese(t3.Years, t3.Months).ShouldBe("< 1 tháng");

        // 1 năm tròn
        var t4 = TenureCalculator.Calculate(new DateTime(2025, 10, 22), baseDate);
        t4.Years.ShouldBe(1);
        t4.Months.ShouldBe(0);
        TenureCalculator.FormatVietnamese(t4.Years, t4.Months).ShouldBe("1 năm");

        // Ngày tương lai
        var t5 = TenureCalculator.Calculate(new DateTime(2026, 11, 1), baseDate);
        t5.TotalMonths.ShouldBe(0);
        TenureCalculator.FormatVietnamese(t5.Years, t5.Months).ShouldBe("< 1 tháng");
    }

    [Fact]
    public async Task Should_Update_Employee_JoinDate_And_Recalculate_Tenure()
    {
        var adminUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "admin");
        var devUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "dev.it");

        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            var newJoinDate = DateTime.UtcNow.AddYears(-1).Date;

            var updated = await EmployeeAppService.UpdateJoinDateAsync(devUser.Id, new UpdateEmployeeJoinDateDto
            {
                JoinDate = newJoinDate
            });

            updated.ShouldNotBeNull();
            updated.RawJoinDate.ShouldNotBeNull();
            updated.RawJoinDate!.Value.Date.ShouldBe(newJoinDate);
            updated.JoinDate.Date.ShouldBe(newJoinDate);
            updated.TenureFormatted.ShouldContain("năm");
        }
    }

    [Fact]
    public async Task Should_Reject_Future_JoinDate()
    {
        var adminUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "admin");
        var devUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "dev.it");

        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            var futureDate = DateTime.UtcNow.AddMonths(2).Date;

            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await EmployeeAppService.UpdateJoinDateAsync(devUser.Id, new UpdateEmployeeJoinDateDto
                {
                    JoinDate = futureDate
                });
            });

            ex.Code.ShouldBe(InternalApprovalDomainErrorCodes.InvalidJoinDate);
        }
    }

    [Fact]
    public async Task Should_Get_Employee_Request_History_With_Filters()
    {
        var adminUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "admin");
        var devUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "dev.it");

        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            // Tất cả đề xuất của dev.it
            var all = await EmployeeAppService.GetRequestsAsync(devUser.Id, new GetEmployeeRequestsInput());
            all.Items.Count.ShouldBeGreaterThan(0);
            all.Items.ShouldAllBe(x => x.CreatorId == devUser.Id);

            // Lọc theo trạng thái Approved
            var approved = await EmployeeAppService.GetRequestsAsync(devUser.Id, new GetEmployeeRequestsInput
            {
                Status = RequestStatus.Approved
            });
            approved.Items.ShouldAllBe(x => x.Status == RequestStatus.Approved);

            // Lọc theo loại đề xuất Leave
            var leaves = await EmployeeAppService.GetRequestsAsync(devUser.Id, new GetEmployeeRequestsInput
            {
                Type = RequestType.Leave
            });
            leaves.Items.ShouldAllBe(x => x.Type == RequestType.Leave);
        }
    }

    [Fact]
    public async Task Should_Calculate_Yearly_Stats_Correctly()
    {
        var adminUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "admin");
        var devUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "dev.it");

        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            var stats = await EmployeeAppService.GetYearlyStatsAsync(devUser.Id, DateTime.UtcNow.Year);

            stats.ShouldNotBeNull();
            stats.TotalRequests.ShouldBeGreaterThan(0);
            stats.BreakdownByType.Count.ShouldBe(3);
            stats.BreakdownByType.ShouldContain(x => x.Type == RequestType.Leave);
            stats.BreakdownByType.ShouldContain(x => x.Type == RequestType.Equipment);
            stats.BreakdownByType.ShouldContain(x => x.Type == RequestType.AdvanceMoney);
        }
    }

    [Fact]
    public async Task Should_Extract_Leave_Timeline_Data_Structured_And_Regex()
    {
        var adminUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "admin");
        var devUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "dev.it");

        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            var timeline = await EmployeeAppService.GetLeaveTimelineAsync(devUser.Id);

            timeline.ShouldNotBeNull();
            timeline.Count.ShouldBeGreaterThan(0);

            var firstLeave = timeline.First();
            firstLeave.StartDate.ShouldNotBe(default);
            firstLeave.EndDate.ShouldNotBe(default);
            firstLeave.Days.ShouldBeGreaterThan(0);
            firstLeave.Reason.ShouldNotBeNullOrWhiteSpace();
            firstLeave.StatusName.ShouldNotBeNullOrWhiteSpace();
        }

        // Kiểm tra LeaveTimelineExtractor với Regex tiếng Việt
        var text = "Tôi xin nghỉ phép 2 ngày từ 25/10 đến 26/10 giải quyết việc riêng";
        var parsed = LeaveTimelineExtractor.Extract("Xin nghỉ phép", text, new DateTime(2026, 10, 20));
        parsed.Days.ShouldBe(2);
        parsed.StartDate.Month.ShouldBe(10);
        parsed.StartDate.Day.ShouldBe(25);
        parsed.EndDate.Day.ShouldBe(26);
    }

    [Fact]
    public async Task Should_Get_Hr_Analytics_Metrics()
    {
        var adminUser = (await UserRepository.GetListAsync()).First(x => x.UserName == "admin");

        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            var analytics = await EmployeeAppService.GetAnalyticsAsync(DateTime.UtcNow.Year);

            analytics.ShouldNotBeNull();
            analytics.CompanySummary.TotalEmployees.ShouldBeGreaterThan(0);
            analytics.CompanySummary.TotalDepartments.ShouldBeGreaterThan(0);
            analytics.CompanySummary.CompanyAverageTenureFormatted.ShouldNotBeNullOrWhiteSpace();

            analytics.HeadcountByDepartment.Count.ShouldBeGreaterThan(0);
            analytics.TenureByDepartment.Count.ShouldBeGreaterThan(0);
            analytics.RequestFrequencyByDepartment.Count.ShouldBeGreaterThan(0);
        }
    }
}
