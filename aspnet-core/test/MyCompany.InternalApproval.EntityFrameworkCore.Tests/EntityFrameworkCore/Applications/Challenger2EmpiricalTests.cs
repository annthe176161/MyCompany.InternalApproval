using System;
using System.Collections.Generic;
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
using Volo.Abp.Security.Claims;
using Xunit;

namespace MyCompany.InternalApproval.EntityFrameworkCore.Applications;

[Collection(InternalApprovalTestConsts.CollectionDefinitionName)]
public class Challenger2EmpiricalTests : InternalApprovalApplicationTestBase<InternalApprovalEntityFrameworkCoreTestModule>
{
    private readonly IEmployeeAppService _employeeAppService;
    private readonly IRequestAppService _requestAppService;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<DepartmentMember, Guid> _memberRepository;
    private readonly IRepository<Request, Guid> _requestRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    public Challenger2EmpiricalTests()
    {
        _employeeAppService = GetRequiredService<IEmployeeAppService>();
        _requestAppService = GetRequiredService<IRequestAppService>();
        _userRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        _departmentRepository = GetRequiredService<IRepository<Department, Guid>>();
        _memberRepository = GetRequiredService<IRepository<DepartmentMember, Guid>>();
        _requestRepository = GetRequiredService<IRepository<Request, Guid>>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    private IDisposable LoginAs(Guid userId, string userName, string role = "admin")
    {
        var claims = new[]
        {
            new Claim(AbpClaimTypes.UserId, userId.ToString()),
            new Claim(AbpClaimTypes.UserName, userName),
            new Claim(AbpClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        return _currentPrincipalAccessor.Change(new ClaimsPrincipal(identity));
    }

    // -------------------------------------------------------------
    // Task 1: Leave Timeline Extraction Stress & Adversarial Tests
    // -------------------------------------------------------------

    [Theory]
    [InlineData("Tôi xin nghỉ từ ngày 1/1 đến 5/1 đi du lịch", 1, 1, 5, 1, 5.0)]
    [InlineData("nghỉ từ ngày 10/05 đến ngày 15/05 giải quyết việc", 10, 5, 15, 5, 6.0)]
    [InlineData("Nghỉ từ 20/8/2026 đến 22/8/2026", 20, 8, 22, 8, 3.0)]
    [InlineData("nghỉ từ ngày 01-02-2026 đến ngày 03-02-2026", 1, 2, 3, 2, 3.0)]
    [InlineData("nghỉ từ 1/8 tới ngày 5/8 đi du lịch", 1, 8, 5, 8, 5.0)]
    [InlineData("nghỉ từ ngày 10/05 đến hết ngày 15/05 giải quyết việc", 10, 5, 15, 5, 6.0)]
    [InlineData("Nghỉ từ 20/8/2026 tới 22/8/2026", 20, 8, 22, 8, 3.0)]
    [InlineData("từ khoảng 11/9 đến ngày 12/9", 11, 9, 12, 9, 2.0)]
    [InlineData("xin nghỉ từ 1/1 tới hết ngày 3/1", 1, 1, 3, 1, 3.0)]
    public void LeaveTimeline_DateRangeRegex_Should_Extract_Valid_Ranges(
        string text, int expectedStartDay, int expectedStartMonth, int expectedEndDay, int expectedEndMonth, double expectedDays)
    {
        var creationTime = new DateTime(2026, 1, 1);
        var result = LeaveTimelineExtractor.Extract("Đơn xin nghỉ phép", text, creationTime);

        result.StartDate.Day.ShouldBe(expectedStartDay);
        result.StartDate.Month.ShouldBe(expectedStartMonth);
        result.EndDate.Day.ShouldBe(expectedEndDay);
        result.EndDate.Month.ShouldBe(expectedEndMonth);
        result.Days.ShouldBe(expectedDays);
    }

    [Theory]
    [InlineData("Tôi muốn xin nghỉ 0.5 ngày vì có việc bận", 0.5)]
    [InlineData("Nghỉ 0,5 ngày buổi chiều", 0.5)]
    [InlineData("Xin nghỉ phép 3 ngày", 3.0)]
    [InlineData("nghỉ 3 ngày phép", 3.0)]
    [InlineData("Xin nghỉ 1.5 ngày", 1.5)]
    [InlineData("Nghỉ 2,5 ngày thăm quê", 2.5)]
    public void LeaveTimeline_DaysCountRegex_Should_Extract_Day_Counts(string text, double expectedDays)
    {
        var creationTime = new DateTime(2026, 6, 1);
        var result = LeaveTimelineExtractor.Extract("Đơn xin nghỉ", text, creationTime);

        result.Days.ShouldBe(expectedDays);
    }

    [Fact]
    public void LeaveTimeline_StructuredData_Takes_Precedence_Over_Text()
    {
        var creationTime = new DateTime(2026, 5, 1);
        var structuredStart = new DateTime(2026, 7, 1);
        var structuredEnd = new DateTime(2026, 7, 10);

        // Văn bản ghi 2 ngày từ 1/1 đến 2/1, nhưng structured ghi 10 ngày từ 1/7 đến 10/7
        var result = LeaveTimelineExtractor.Extract(
            "Nghỉ phép",
            "Nghỉ 2 ngày từ ngày 1/1 đến 2/1",
            creationTime,
            structuredStart,
            structuredEnd);

        result.StartDate.Date.ShouldBe(structuredStart.Date);
        result.EndDate.Date.ShouldBe(structuredEnd.Date);
        result.Days.ShouldBe(10.0);
    }

    [Fact]
    public void LeaveTimeline_Fallback_When_No_Dates_In_Text()
    {
        var creationTime = new DateTime(2026, 4, 15);
        var result = LeaveTimelineExtractor.Extract("Xin nghỉ phép", "Tôi có việc gia đình đột xuất", creationTime);

        result.StartDate.Date.ShouldBe(creationTime.Date);
        result.EndDate.Date.ShouldBe(creationTime.Date);
        result.Days.ShouldBe(1.0); // Default to 1 day
    }

    [Fact]
    public void LeaveTimeline_InvalidDate_Like_Feb31_Should_Fallback_Safely()
    {
        var creationTime = new DateTime(2026, 2, 1);
        // Ngày 31/02 không tồn tại trong lịch
        var result = LeaveTimelineExtractor.Extract("Xin nghỉ phép", "Tôi xin nghỉ từ ngày 31/02 đến ngày 05/03", creationTime);

        result.ShouldNotBeNull();
        result.StartDate.ShouldNotBe(default);
        result.EndDate.ShouldNotBe(default);
    }

    [Fact]
    public void LeaveTimeline_Cross_Year_Explicit()
    {
        var creationTime = new DateTime(2025, 12, 20);
        var result = LeaveTimelineExtractor.Extract("Nghỉ Tết", "Tôi xin nghỉ từ ngày 28/12/2025 đến ngày 02/01/2026", creationTime);

        result.StartDate.Date.ShouldBe(new DateTime(2025, 12, 28));
        result.EndDate.Date.ShouldBe(new DateTime(2026, 1, 2));
        result.Days.ShouldBe(6.0);
    }

    // -------------------------------------------------------------
    // Task 2: Yearly Statistics Accounting Invariants
    // -------------------------------------------------------------

    [Fact]
    public async Task YearlyStats_Invariant_TotalRequests_Equals_Sum_Of_Statuses()
    {
        var adminUser = (await _userRepository.GetListAsync()).First(x => x.UserName == "admin");
        var devUser = (await _userRepository.GetListAsync()).First(x => x.UserName == "dev.it");

        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            var stats = await _employeeAppService.GetYearlyStatsAsync(devUser.Id, DateTime.UtcNow.Year);

            stats.ShouldNotBeNull();
            var sumOfStatuses = stats.DraftCount + stats.SubmittedCount + stats.ApprovedCount + stats.RejectedCount;
            stats.TotalRequests.ShouldBe(sumOfStatuses);
        }
    }

    [Fact]
    public async Task YearlyStats_Invariant_TotalDisbursed_Is_Less_Or_Equal_To_TotalRequested()
    {
        var adminUser = (await _userRepository.GetListAsync()).First(x => x.UserName == "admin");
        var users = await _userRepository.GetListAsync();

        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            foreach (var user in users)
            {
                var stats = await _employeeAppService.GetYearlyStatsAsync(user.Id, DateTime.UtcNow.Year);
                stats.TotalAmountDisbursed.ShouldBeLessThanOrEqualTo(stats.TotalAmountRequested);
            }
        }
    }

    [Fact]
    public async Task YearlyStats_LeaveDaysTotal_Should_Exclude_Unapproved_Leaves_Investigation()
    {
        var adminUser = (await _userRepository.GetListAsync()).First(x => x.UserName == "admin");
        var devUser = (await _userRepository.GetListAsync()).First(x => x.UserName == "dev.it");
        var managerUser = (await _userRepository.GetListAsync()).First(x => x.UserName == "manager.it");

        double initialLeaveDays;
        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            var initialStats = await _employeeAppService.GetYearlyStatsAsync(devUser.Id, DateTime.UtcNow.Year);
            initialLeaveDays = initialStats.LeaveDaysTotal;
        }

        Guid draftId;
        Guid rejectedId;

        // Tạo 1 đề xuất DRAFT (3 ngày) dưới tên dev.it
        using (LoginAs(devUser.Id, "dev.it", "Employee"))
        {
            var draftReq = await _requestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Bản nháp nghỉ 3 ngày",
                Type = RequestType.Leave,
                Description = "Tôi xin nghỉ 3 ngày",
                StartDate = DateTime.UtcNow.AddDays(20).Date,
                EndDate = DateTime.UtcNow.AddDays(22).Date
            });
            draftId = draftReq.Id;

            // Tạo 1 đề xuất khác và submit để reject
            var toReject = await _requestAppService.CreateAsync(new CreateRequestDto
            {
                Title = "Nghỉ phép 5 ngày bị từ chối",
                Type = RequestType.Leave,
                Description = "Tôi xin nghỉ 5 ngày",
                StartDate = DateTime.UtcNow.AddDays(30).Date,
                EndDate = DateTime.UtcNow.AddDays(34).Date
            });
            await _requestAppService.SubmitAsync(toReject.Id);
            rejectedId = toReject.Id;
        }

        // Manager từ chối đề xuất rejectedId
        using (LoginAs(managerUser.Id, "manager.it", "Manager"))
        {
            await _requestAppService.RejectAsync(rejectedId, new RejectRequestDto
            {
                Reason = "Trùng lịch bảo trì hệ thống"
            });
        }

        // Lấy lại thống kê năm bằng quyền admin
        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            var newStats = await _employeeAppService.GetYearlyStatsAsync(devUser.Id, DateTime.UtcNow.Year);

            // Ghi nhận thực nghiệm
            Console.WriteLine($"[EMPIRICAL OBS] Initial Leave Days: {initialLeaveDays}");
            Console.WriteLine($"[EMPIRICAL OBS] New Leave Days (after 1 draft of 3d and 1 rejected of 5d): {newStats.LeaveDaysTotal}");
            Console.WriteLine($"[EMPIRICAL OBS] Difference in LeaveDaysTotal: {newStats.LeaveDaysTotal - initialLeaveDays}");

            // TEST SPECIFICATION (TEST_READY.md line 126-127 & stats-oracle.js line 62):
            // "LeaveDaysTotal == Sum(Days of Approved Leave requests in target year)"
            // "Unapproved leave requests are excluded from LeaveDaysTotal."
            // If the code counts draft (3) + rejected (5), it added 8 days!
            var unapprovedDaysIncluded = newStats.LeaveDaysTotal - initialLeaveDays;
            Console.WriteLine($"[EMPIRICAL FINDING] Unapproved leave days counted into LeaveDaysTotal: {unapprovedDaysIncluded}");

            // We assert whether unapproved days are properly excluded:
            newStats.LeaveDaysTotal.ShouldBe(initialLeaveDays,
                $"Domain Invariant Violation: YearlyStats.LeaveDaysTotal included unapproved leave requests (draft: 3 days, rejected: 5 days, extra: {unapprovedDaysIncluded} days)!");
        }
    }

    // -------------------------------------------------------------
    // Task 3: HR Analytics Invariants Investigation
    // -------------------------------------------------------------

    [Fact]
    public async Task HrAnalytics_Headcount_Invariant_Investigation()
    {
        var adminUser = (await _userRepository.GetListAsync()).First(x => x.UserName == "admin");

        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            var analytics = await _employeeAppService.GetAnalyticsAsync(DateTime.UtcNow.Year);

            var totalEmployees = analytics.CompanySummary.TotalEmployees;
            var sumHeadcounts = analytics.HeadcountByDepartment.Sum(x => x.Headcount);

            Console.WriteLine($"[EMPIRICAL OBS] TotalEmployees (AbpUsers): {totalEmployees}");
            Console.WriteLine($"[EMPIRICAL OBS] Sum of Department Headcounts: {sumHeadcounts}");

            // SPEC INVARIANT (TEST_READY.md line 130):
            // "Sum(DepartmentHeadcount) == TotalEmployees"
            sumHeadcounts.ShouldBe(totalEmployees,
                $"Domain Invariant Violation: Sum of Department headcounts ({sumHeadcounts}) does not equal TotalEmployees ({totalEmployees}) because unassigned users like admin are not accounted for!");
        }
    }

    [Fact]
    public async Task HrAnalytics_Percentage_Invariant_Investigation()
    {
        var adminUser = (await _userRepository.GetListAsync()).First(x => x.UserName == "admin");

        using (LoginAs(adminUser.Id, "admin", "admin"))
        {
            var analytics = await _employeeAppService.GetAnalyticsAsync(DateTime.UtcNow.Year);

            var sumPercentages = analytics.HeadcountByDepartment.Sum(x => x.Percentage);
            Console.WriteLine($"[EMPIRICAL OBS] Sum of Department Percentages: {sumPercentages}%");

            foreach (var dept in analytics.HeadcountByDepartment)
            {
                Console.WriteLine($"[EMPIRICAL OBS] Dept {dept.DepartmentCode}: Headcount={dept.Headcount}, Percentage={dept.Percentage}%");
            }

            // SPEC INVARIANT (TEST_READY.md line 131):
            // "Sum(DepartmentPercentage) == 100.0% (+/- 0.2% rounding)"
            var diff = Math.Abs(sumPercentages - 100.0);
            diff.ShouldBeLessThanOrEqualTo(0.2,
                $"Domain Invariant Violation: Sum of Department percentages is {sumPercentages}%, which differs from 100% by {diff}% (tolerance is 0.2%)!");
        }
    }
}
