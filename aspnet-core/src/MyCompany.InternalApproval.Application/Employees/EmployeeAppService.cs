using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MyCompany.InternalApproval.Departments;
using MyCompany.InternalApproval.Permissions;
using MyCompany.InternalApproval.Requests;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// Dịch vụ ứng dụng quản lý Hồ sơ Nhân viên, Thâm niên làm việc,
/// Timeline nghỉ phép và Thống kê & Phân tích nhân sự.
/// </summary>
[Authorize(InternalApprovalPermissions.Employees.Default)]
[RemoteService(IsEnabled = false)]
public class EmployeeAppService : InternalApprovalAppService, IEmployeeAppService
{
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<DepartmentMember, Guid> _memberRepository;
    private readonly IRepository<Request, Guid> _requestRepository;
    private readonly IPermissionManager _permissionManager;

    public EmployeeAppService(
        IRepository<IdentityUser, Guid> userRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<DepartmentMember, Guid> memberRepository,
        IRepository<Request, Guid> requestRepository,
        IPermissionManager permissionManager)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _memberRepository = memberRepository;
        _requestRepository = requestRepository;
        _permissionManager = permissionManager;
    }

    protected class EmployeeAccessScope
    {
        public bool IsGlobal { get; }
        public bool IsManager { get; }
        public Guid? CurrentUserId { get; }
        public HashSet<Guid> ManagedDeptIds { get; }

        public EmployeeAccessScope(bool isGlobal, bool isManager, Guid? currentUserId, HashSet<Guid> managedDeptIds)
        {
            IsGlobal = isGlobal;
            IsManager = isManager;
            CurrentUserId = currentUserId;
            ManagedDeptIds = managedDeptIds ?? new HashSet<Guid>();
        }
    }

    /// <summary>
    /// Đánh giá phạm vi quyền hạn dữ liệu của người dùng hiện tại (Admin / Trưởng phòng / Nhân viên).
    /// </summary>
    protected virtual async Task<EmployeeAccessScope> EvaluateCurrentUserAccessScopeAsync()
    {
        var currentUserId = CurrentUser.Id;
        var isGlobal = CurrentUser.IsInRole("admin")
            || (CurrentUser.UserName != null && CurrentUser.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase));

        if (!isGlobal && currentUserId.HasValue)
        {
            // Kiểm tra phân quyền Employees.ViewAll trực tiếp cho user hoặc qua role
            var userGrant = await _permissionManager.GetAsync(
                InternalApprovalPermissions.Employees.ViewAll,
                UserPermissionValueProvider.ProviderName,
                currentUserId.Value.ToString());

            if (userGrant?.IsGranted == true)
            {
                isGlobal = true;
            }
            else if (CurrentUser.Roles != null && CurrentUser.Roles.Length > 0)
            {
                foreach (var role in CurrentUser.Roles)
                {
                    var roleGrant = await _permissionManager.GetAsync(
                        InternalApprovalPermissions.Employees.ViewAll,
                        RolePermissionValueProvider.ProviderName,
                        role);

                    if (roleGrant?.IsGranted == true)
                    {
                        isGlobal = true;
                        break;
                    }
                }
            }
        }

        if (isGlobal)
        {
            return new EmployeeAccessScope(true, false, currentUserId, new HashSet<Guid>());
        }

        if (currentUserId.HasValue)
        {
            var allDepts = await _departmentRepository.GetListAsync();
            var managedDeptIds = allDepts
                .Where(d => d.ManagerId == currentUserId.Value)
                .Select(d => d.Id)
                .ToHashSet();

            if (managedDeptIds.Count > 0)
            {
                return new EmployeeAccessScope(false, true, currentUserId, managedDeptIds);
            }
        }

        return new EmployeeAccessScope(false, false, currentUserId, new HashSet<Guid>());
    }

    /// <summary>
    /// Kiểm tra quyền truy cập thông tin của một nhân sự mục tiêu theo cấu trúc phân quyền phân cấp.
    /// Ném AbpAuthorizationException nếu truy cập trái phép.
    /// </summary>
    protected virtual async Task CheckCanAccessEmployeeAsync(Guid targetUserId)
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            throw new AbpAuthorizationException("Người dùng chưa đăng nhập.");
        }

        // 1. Luôn cho phép truy cập thông tin của chính mình
        if (targetUserId == currentUserId.Value)
        {
            return;
        }

        var scope = await EvaluateCurrentUserAccessScopeAsync();

        // 2. Quyền toàn hệ thống (Admin / Employees.ViewAll)
        if (scope.IsGlobal)
        {
            return;
        }

        // 3. Trưởng phòng: cho phép nếu nhân viên thuộc phòng ban mình quản lý
        if (scope.IsManager)
        {
            var targetMember = await _memberRepository.FirstOrDefaultAsync(m => m.UserId == targetUserId);
            if (targetMember != null && scope.ManagedDeptIds.Contains(targetMember.DepartmentId))
            {
                return;
            }
        }

        // 4. Bị chặn truy cập chéo phòng ban / cán bộ khác
        throw new AbpAuthorizationException("Bạn không có quyền truy cập thông tin của nhân sự này.");
    }

    /// <summary>
    /// Lấy danh sách nhân viên có phân trang, tìm kiếm họ tên/email/vị trí và lọc theo phòng ban, thâm niên.
    /// </summary>
    public virtual async Task<PagedResultDto<EmployeeListItemDto>> GetListAsync(GetEmployeeListInput input)
    {
        var scope = await EvaluateCurrentUserAccessScopeAsync();

        // Nếu là Manager và chọn lọc theo phòng ban không thuộc quyền quản lý -> throw AbpAuthorizationException
        if (scope.IsManager && input.DepartmentId.HasValue && !scope.ManagedDeptIds.Contains(input.DepartmentId.Value))
        {
            throw new AbpAuthorizationException("Bạn không có quyền truy cập dữ liệu của phòng ban này.");
        }

        var users = await _userRepository.GetListAsync();
        var members = await _memberRepository.GetListAsync();
        var departments = await _departmentRepository.GetListAsync();

        var currentYear = DateTime.UtcNow.Year;
        var requestQuery = await _requestRepository.GetQueryableAsync();
        var currentYearRequests = await AsyncExecuter.ToListAsync(
            requestQuery.Where(x => x.CreationTime.Year == currentYear && x.CreatorId.HasValue));

        var requestCountDict = currentYearRequests
            .GroupBy(x => x.CreatorId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var memberDict = members.ToDictionary(x => x.UserId, x => x);
        var deptDict = departments.ToDictionary(x => x.Id, x => x);

        var employeeList = new List<EmployeeListItemDto>();

        foreach (var user in users)
        {
            memberDict.TryGetValue(user.Id, out var member);
            Department? dept = null;
            if (member != null && deptDict.TryGetValue(member.DepartmentId, out var d))
            {
                dept = d;
            }

            // Kiểm tra phân quyền truy cập theo phân cấp
            if (!scope.IsGlobal)
            {
                if (scope.IsManager)
                {
                    var isManagedDeptMember = member != null && scope.ManagedDeptIds.Contains(member.DepartmentId);
                    var isSelf = user.Id == scope.CurrentUserId;
                    if (!isManagedDeptMember && !isSelf)
                    {
                        continue;
                    }
                }
                else
                {
                    // Regular employee chỉ thấy chính mình
                    if (user.Id != scope.CurrentUserId)
                    {
                        continue;
                    }
                }
            }

            var effectiveJoinDate = TenureCalculator.GetEffectiveJoinDate(member?.JoinDate, user.CreationTime);
            var (years, months, totalMonths) = TenureCalculator.Calculate(effectiveJoinDate);
            var tenureFormatted = TenureCalculator.FormatVietnamese(years, months);

            var fullName = (!string.IsNullOrWhiteSpace(user.Surname) ? user.Surname + " " : "") + user.Name;
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = user.UserName;
            }

            requestCountDict.TryGetValue(user.Id, out var reqCount);

            var item = new EmployeeListItemDto
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = fullName.Trim(),
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DepartmentId = member?.DepartmentId,
                DepartmentCode = dept?.Code,
                DepartmentName = dept?.Name,
                Position = member?.Position ?? string.Empty,
                IsManager = dept != null && dept.ManagerId == user.Id,
                JoinDate = effectiveJoinDate,
                RawJoinDate = member?.JoinDate,
                CreationTime = user.CreationTime,
                TenureMonths = totalMonths,
                TenureFormatted = tenureFormatted,
                CurrentYearRequestCount = reqCount
            };

            // Lọc theo phòng ban
            if (input.DepartmentId.HasValue && item.DepartmentId != input.DepartmentId.Value)
            {
                continue;
            }

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                var f = input.Filter.Trim().ToLowerInvariant();
                var match = item.FullName.ToLowerInvariant().Contains(f)
                    || item.UserName.ToLowerInvariant().Contains(f)
                    || item.Email.ToLowerInvariant().Contains(f)
                    || item.Position.ToLowerInvariant().Contains(f)
                    || (item.DepartmentName != null && item.DepartmentName.ToLowerInvariant().Contains(f));

                if (!match)
                {
                    continue;
                }
            }

            // Lọc theo thâm niên tối thiểu / tối đa (tháng)
            if (input.MinTenureMonths.HasValue && item.TenureMonths < input.MinTenureMonths.Value)
            {
                continue;
            }

            if (input.MaxTenureMonths.HasValue && item.TenureMonths > input.MaxTenureMonths.Value)
            {
                continue;
            }

            employeeList.Add(item);
        }

        var totalCount = employeeList.Count;

        // Sắp xếp mặc định hoặc theo Sorting
        var pagedItems = employeeList
            .OrderByDescending(x => x.IsManager)
            .ThenBy(x => x.FullName)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return new PagedResultDto<EmployeeListItemDto>(totalCount, pagedItems);
    }

    /// <summary>
    /// Lấy chi tiết hồ sơ nhân viên, thâm niên làm việc, thống kê trong năm và các đề xuất gần nhất.
    /// </summary>
    public virtual async Task<EmployeeProfileDto> GetAsync(Guid id)
    {
        var user = await _userRepository.FindAsync(id);
        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), id);
        }

        await CheckCanAccessEmployeeAsync(id);

        var member = await _memberRepository.FirstOrDefaultAsync(x => x.UserId == id);
        Department? dept = null;
        if (member != null)
        {
            dept = await _departmentRepository.FindAsync(member.DepartmentId);
        }

        var effectiveJoinDate = TenureCalculator.GetEffectiveJoinDate(member?.JoinDate, user.CreationTime);
        var (years, months, totalMonths) = TenureCalculator.Calculate(effectiveJoinDate);
        var tenureFormatted = TenureCalculator.FormatVietnamese(years, months);

        var fullName = (!string.IsNullOrWhiteSpace(user.Surname) ? user.Surname + " " : "") + user.Name;
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = user.UserName;
        }

        var currentYear = DateTime.UtcNow.Year;
        var yearlyStats = await CalculateYearlyStatsAsync(id, currentYear);

        // Lấy 10 đề xuất gần nhất của nhân viên
        var requestQuery = await _requestRepository.GetQueryableAsync();
        var recentEntities = await AsyncExecuter.ToListAsync(
            requestQuery
                .Where(x => x.CreatorId == id)
                .OrderByDescending(x => x.CreationTime)
                .Take(10));

        var recentDtos = ObjectMapper.Map<List<Request>, List<RequestDto>>(recentEntities);
        foreach (var rDto in recentDtos)
        {
            rDto.CreatorName = fullName.Trim();
            rDto.DepartmentName = dept?.Name;
            rDto.DepartmentCode = dept?.Code;
        }

        return new EmployeeProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = fullName.Trim(),
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DepartmentId = member?.DepartmentId,
            DepartmentCode = dept?.Code,
            DepartmentName = dept?.Name,
            Position = member?.Position ?? string.Empty,
            IsManager = dept != null && dept.ManagerId == user.Id,
            JoinDate = effectiveJoinDate,
            RawJoinDate = member?.JoinDate,
            CreationTime = user.CreationTime,
            TenureMonths = totalMonths,
            TenureFormatted = tenureFormatted,
            CurrentYearRequestCount = yearlyStats.TotalRequests,
            CurrentYearStats = yearlyStats,
            RecentRequests = recentDtos
        };
    }

    /// <summary>
    /// Cập nhật ngày bắt đầu làm việc của nhân viên và tính toán lại thâm niên.
    /// </summary>
    [Authorize(InternalApprovalPermissions.Employees.Edit)]
    public virtual async Task<EmployeeProfileDto> UpdateJoinDateAsync(Guid id, UpdateEmployeeJoinDateDto input)
    {
        // Kiểm tra ràng buộc hợp lệ: không được là ngày trong tương lai và không trước năm 1970
        if (input.JoinDate.Date > DateTime.UtcNow.Date.AddDays(1))
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.InvalidJoinDate);
        }

        if (input.JoinDate < new DateTime(1970, 1, 1))
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.InvalidJoinDate);
        }

        var user = await _userRepository.FindAsync(id);
        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), id);
        }

        await CheckCanAccessEmployeeAsync(id);

        var member = await _memberRepository.FirstOrDefaultAsync(x => x.UserId == id);
        if (member != null)
        {
            member.JoinDate = input.JoinDate.Date;
            await _memberRepository.UpdateAsync(member, autoSave: true);
        }
        else
        {
            // Nếu chưa có phòng ban, tạo liên kết dự phòng với phòng ban đầu tiên hoặc lưu tạm
            var firstDept = await _departmentRepository.FirstOrDefaultAsync();
            if (firstDept != null)
            {
                var newMember = new DepartmentMember(
                    GuidGenerator.Create(),
                    firstDept.Id,
                    id,
                    "Nhân viên",
                    input.JoinDate.Date
                );
                await _memberRepository.InsertAsync(newMember, autoSave: true);
            }
        }

        return await GetAsync(id);
    }

    /// <summary>
    /// Lấy lịch sử đề xuất của nhân viên kèm bộ lọc theo trạng thái, loại hình và năm.
    /// </summary>
    public virtual async Task<PagedResultDto<RequestDto>> GetRequestsAsync(Guid id, GetEmployeeRequestsInput input)
    {
        var user = await _userRepository.FindAsync(id);
        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), id);
        }

        await CheckCanAccessEmployeeAsync(id);

        var fullName = ((!string.IsNullOrWhiteSpace(user.Surname) ? user.Surname + " " : "") + user.Name).Trim();

        var member = await _memberRepository.FirstOrDefaultAsync(x => x.UserId == id);
        Department? dept = null;
        if (member != null)
        {
            dept = await _departmentRepository.FindAsync(member.DepartmentId);
        }

        var query = await _requestRepository.GetQueryableAsync();
        query = query.Where(x => x.CreatorId == id);

        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        if (input.Type.HasValue)
        {
            query = query.Where(x => x.Type == input.Type.Value);
        }

        if (input.Year.HasValue)
        {
            query = query.Where(x => x.CreationTime.Year == input.Year.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var requests = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(x => x.CreationTime)
                 .Skip(input.SkipCount)
                 .Take(input.MaxResultCount));

        // Lấy danh sách Approvers để hiển thị tên người duyệt
        var approverIds = requests.Where(x => x.ApproverId.HasValue).Select(x => x.ApproverId!.Value).Distinct().ToList();
        var approvers = approverIds.Any()
            ? await _userRepository.GetListAsync(x => approverIds.Contains(x.Id))
            : new List<IdentityUser>();
        var approverDict = approvers.ToDictionary(
            x => x.Id,
            x => (!string.IsNullOrWhiteSpace(x.Surname) ? x.Surname + " " : "") + x.Name);

        var dtos = ObjectMapper.Map<List<Request>, List<RequestDto>>(requests);
        foreach (var dto in dtos)
        {
            dto.CreatorName = fullName;
            dto.DepartmentName = dept?.Name;
            dto.DepartmentCode = dept?.Code;

            if (dto.ApproverId.HasValue && approverDict.TryGetValue(dto.ApproverId.Value, out var approverName))
            {
                dto.ApproverName = approverName;
            }
        }

        return new PagedResultDto<RequestDto>(totalCount, dtos);
    }

    /// <summary>
    /// Thống kê đề xuất theo năm của nhân viên: số lượng theo trạng thái, tổng tiền yêu cầu, tổng tiền giải ngân và ngày nghỉ.
    /// </summary>
    public virtual async Task<EmployeeYearlyStatsDto> GetYearlyStatsAsync(Guid id, int? year = null)
    {
        var user = await _userRepository.FindAsync(id);
        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), id);
        }

        await CheckCanAccessEmployeeAsync(id);

        var targetYear = year ?? DateTime.UtcNow.Year;
        return await CalculateYearlyStatsAsync(id, targetYear);
    }

    /// <summary>
    /// Lấy danh sách các đợt nghỉ phép đã bóc tách thời gian, số ngày và lý do phục vụ timeline nghỉ phép trực quan.
    /// </summary>
    public virtual async Task<List<LeaveTimelineItemDto>> GetLeaveTimelineAsync(Guid id, int? year = null)
    {
        var user = await _userRepository.FindAsync(id);
        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), id);
        }

        await CheckCanAccessEmployeeAsync(id);

        var query = await _requestRepository.GetQueryableAsync();
        var leaves = await AsyncExecuter.ToListAsync(
            query.Where(x => x.CreatorId == id && x.Type == RequestType.Leave));

        // Lấy danh sách người duyệt
        var approverIds = leaves.Where(x => x.ApproverId.HasValue).Select(x => x.ApproverId!.Value).Distinct().ToList();
        var approvers = approverIds.Any()
            ? await _userRepository.GetListAsync(x => approverIds.Contains(x.Id))
            : new List<IdentityUser>();
        var approverDict = approvers.ToDictionary(
            x => x.Id,
            x => (!string.IsNullOrWhiteSpace(x.Surname) ? x.Surname + " " : "") + x.Name);

        var timeline = new List<LeaveTimelineItemDto>();

        foreach (var req in leaves)
        {
            var extracted = LeaveTimelineExtractor.Extract(req);

            if (year.HasValue && extracted.StartDate.Year != year.Value && req.CreationTime.Year != year.Value)
            {
                continue;
            }

            approverDict.TryGetValue(req.ApproverId ?? Guid.Empty, out var approverName);

            timeline.Add(new LeaveTimelineItemDto
            {
                RequestId = req.Id,
                Title = req.Title,
                StartDate = extracted.StartDate,
                EndDate = extracted.EndDate,
                Days = extracted.Days,
                Reason = extracted.Reason,
                Status = req.Status,
                StatusName = GetStatusName(req.Status),
                CreationTime = req.CreationTime,
                ApproverName = approverName,
                ApprovalTime = req.ApprovalTime,
                RejectionReason = req.RejectionReason
            });
        }

        return timeline.OrderByDescending(x => x.StartDate).ThenByDescending(x => x.CreationTime).ToList();
    }

    /// <summary>
    /// Lấy dữ liệu phân tích và thống kê tổng thể nhân sự: phân bổ nhân sự theo phòng ban,
    /// thâm niên trung bình của các bộ phận, và tần suất gửi đề xuất của nhân viên.
    /// </summary>
    [Authorize(InternalApprovalPermissions.Employees.Analytics)]
    public virtual async Task<HrAnalyticsDto> GetAnalyticsAsync(int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var scope = await EvaluateCurrentUserAccessScopeAsync();

        var users = await _userRepository.GetListAsync();
        var departments = await _departmentRepository.GetListAsync();
        var members = await _memberRepository.GetListAsync();

        if (!scope.IsGlobal)
        {
            if (scope.IsManager)
            {
                departments = departments.Where(d => scope.ManagedDeptIds.Contains(d.Id)).ToList();
                members = members.Where(m => scope.ManagedDeptIds.Contains(m.DepartmentId)).ToList();
                var managedUserIds = members.Select(m => m.UserId).ToHashSet();
                users = users.Where(u => managedUserIds.Contains(u.Id)).ToList();
            }
            else
            {
                var selfMember = members.FirstOrDefault(m => m.UserId == scope.CurrentUserId);
                if (selfMember != null)
                {
                    departments = departments.Where(d => d.Id == selfMember.DepartmentId).ToList();
                    members = members.Where(m => m.DepartmentId == selfMember.DepartmentId).ToList();
                    var deptUserIds = members.Select(m => m.UserId).ToHashSet();
                    users = users.Where(u => deptUserIds.Contains(u.Id)).ToList();
                }
                else
                {
                    departments = new List<Department>();
                    members = new List<DepartmentMember>();
                    users = new List<IdentityUser>();
                }
            }
        }

        var requestQuery = await _requestRepository.GetQueryableAsync();
        var allRequests = await AsyncExecuter.ToListAsync(
            requestQuery.Where(x => x.CreationTime.Year == targetYear));

        if (!scope.IsGlobal)
        {
            var allowedUserIds = members.Select(m => m.UserId).ToHashSet();
            allRequests = allRequests.Where(r => r.CreatorId.HasValue && allowedUserIds.Contains(r.CreatorId.Value)).ToList();
        }

        var memberDict = members.ToDictionary(x => x.UserId, x => x);
        var deptDict = departments.ToDictionary(x => x.Id, x => x);

        // 1. Company Summary
        var assignedDeptIds = departments.Select(d => d.Id).ToHashSet();
        var assignedMembers = members.Where(m => assignedDeptIds.Contains(m.DepartmentId)).ToList();
        var assignedUserIds = assignedMembers.Select(x => x.UserId).ToHashSet();
        var assignedUsers = users.Where(u => assignedUserIds.Contains(u.Id)).ToList();
        var totalEmployees = assignedUsers.Count;
        var totalDepartments = departments.Count;

        var tenureMonthsList = new List<int>();
        foreach (var u in assignedUsers)
        {
            memberDict.TryGetValue(u.Id, out var m);
            var effectiveJoin = TenureCalculator.GetEffectiveJoinDate(m?.JoinDate, u.CreationTime);
            var (_, _, totalM) = TenureCalculator.Calculate(effectiveJoin);
            tenureMonthsList.Add(totalM);
        }

        var avgCompanyTenureMonths = tenureMonthsList.Any() ? tenureMonthsList.Average() : 0.0;
        var totalRequestsThisYear = allRequests.Count;
        var totalDisbursedThisYear = allRequests
            .Where(x => x.Status == RequestStatus.Approved && x.Amount.HasValue)
            .Sum(x => x.Amount!.Value);

        var result = new HrAnalyticsDto
        {
            Year = targetYear,
            CompanySummary = new CompanyHrSummaryDto
            {
                TotalEmployees = totalEmployees,
                TotalDepartments = totalDepartments,
                CompanyAverageTenureMonths = Math.Round(avgCompanyTenureMonths, 1),
                CompanyAverageTenureFormatted = TenureCalculator.FormatAverageMonths(avgCompanyTenureMonths),
                TotalRequestsThisYear = totalRequestsThisYear,
                TotalDisbursedAmountThisYear = totalDisbursedThisYear
            }
        };

        // 2. Headcount & Tenure & Request Frequency by Department
        var userDeptLookup = members.ToDictionary(x => x.UserId, x => x.DepartmentId);
        var requestsByDept = allRequests
            .Where(x => x.CreatorId.HasValue && userDeptLookup.ContainsKey(x.CreatorId!.Value))
            .GroupBy(x => userDeptLookup[x.CreatorId!.Value])
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var dept in departments)
        {
            var deptMembers = members.Where(x => x.DepartmentId == dept.Id).ToList();
            var memberUserIds = deptMembers.Select(x => x.UserId).ToHashSet();
            var deptUsers = users.Where(x => memberUserIds.Contains(x.Id)).ToList();

            var headcount = deptUsers.Count;
            var percentage = totalEmployees > 0 ? Math.Round((double)headcount / totalEmployees * 100, 1) : 0;

            result.HeadcountByDepartment.Add(new DepartmentHeadcountDto
            {
                DepartmentId = dept.Id,
                DepartmentCode = dept.Code,
                DepartmentName = dept.Name,
                Headcount = headcount,
                Percentage = percentage
            });

            // Department Tenure
            var deptTenures = deptUsers.Select(u =>
            {
                memberDict.TryGetValue(u.Id, out var m);
                var effectiveJoin = TenureCalculator.GetEffectiveJoinDate(m?.JoinDate, u.CreationTime);
                return TenureCalculator.Calculate(effectiveJoin).TotalMonths;
            }).ToList();

            var avgDeptTenure = deptTenures.Any() ? deptTenures.Average() : 0.0;
            var minTenure = deptTenures.Any() ? deptTenures.Min() : 0;
            var maxTenure = deptTenures.Any() ? deptTenures.Max() : 0;

            result.TenureByDepartment.Add(new DepartmentTenureDto
            {
                DepartmentId = dept.Id,
                DepartmentCode = dept.Code,
                DepartmentName = dept.Name,
                AverageTenureMonths = Math.Round(avgDeptTenure, 1),
                AverageTenureFormatted = TenureCalculator.FormatAverageMonths(avgDeptTenure),
                MinTenureMonths = minTenure,
                MaxTenureMonths = maxTenure
            });

            // Request frequency
            requestsByDept.TryGetValue(dept.Id, out var deptRequests);
            var reqCount = deptRequests?.Count ?? 0;
            var reqPerEmp = headcount > 0 ? Math.Round((double)reqCount / headcount, 2) : 0;

            result.RequestFrequencyByDepartment.Add(new DepartmentRequestFrequencyDto
            {
                DepartmentId = dept.Id,
                DepartmentCode = dept.Code,
                DepartmentName = dept.Name,
                TotalRequests = reqCount,
                RequestsPerEmployee = reqPerEmp
            });
        }

        // 3. Top Requesters
        var userDict = users.ToDictionary(x => x.Id, x => x);
        var topGroups = allRequests
            .Where(x => x.CreatorId.HasValue)
            .GroupBy(x => x.CreatorId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(5);

        foreach (var g in topGroups)
        {
            if (userDict.TryGetValue(g.Key, out var u))
            {
                memberDict.TryGetValue(u.Id, out var m);
                Department? d = null;
                if (m != null && deptDict.TryGetValue(m.DepartmentId, out var foundDept))
                {
                    d = foundDept;
                }

                var uName = (!string.IsNullOrWhiteSpace(u.Surname) ? u.Surname + " " : "") + u.Name;
                result.TopRequesters.Add(new TopRequesterDto
                {
                    UserId = u.Id,
                    FullName = !string.IsNullOrWhiteSpace(uName) ? uName.Trim() : u.UserName,
                    DepartmentName = d?.Name ?? "N/A",
                    RequestCount = g.Count()
                });
            }
        }

        return result;
    }

    private async Task<EmployeeYearlyStatsDto> CalculateYearlyStatsAsync(Guid userId, int targetYear)
    {
        var requestQuery = await _requestRepository.GetQueryableAsync();
        var requests = await AsyncExecuter.ToListAsync(
            requestQuery.Where(x => x.CreatorId == userId && x.CreationTime.Year == targetYear));

        var leaveRequests = requests.Where(x => x.Type == RequestType.Leave).ToList();
        var approvedLeaveRequests = leaveRequests.Where(x => x.Status == RequestStatus.Approved).ToList();
        double totalLeaveDays = 0;
        foreach (var lr in approvedLeaveRequests)
        {
            var extracted = LeaveTimelineExtractor.Extract(lr);
            totalLeaveDays += extracted.Days;
        }

        var totalAmountRequested = requests
            .Where(x => x.Amount.HasValue)
            .Sum(x => x.Amount!.Value);

        var totalAmountDisbursed = requests
            .Where(x => x.Status == RequestStatus.Approved && x.Amount.HasValue)
            .Sum(x => x.Amount!.Value);

        var typeBreakdowns = new List<RequestTypeStatDto>
        {
            new RequestTypeStatDto
            {
                Type = RequestType.Leave,
                TypeName = "Nghỉ phép",
                Count = leaveRequests.Count,
                TotalAmount = 0
            },
            new RequestTypeStatDto
            {
                Type = RequestType.Equipment,
                TypeName = "Mua sắm thiết bị",
                Count = requests.Count(x => x.Type == RequestType.Equipment),
                TotalAmount = requests.Where(x => x.Type == RequestType.Equipment && x.Amount.HasValue).Sum(x => x.Amount!.Value)
            },
            new RequestTypeStatDto
            {
                Type = RequestType.AdvanceMoney,
                TypeName = "Tạm ứng tiền",
                Count = requests.Count(x => x.Type == RequestType.AdvanceMoney),
                TotalAmount = requests.Where(x => x.Type == RequestType.AdvanceMoney && x.Amount.HasValue).Sum(x => x.Amount!.Value)
            }
        };

        return new EmployeeYearlyStatsDto
        {
            Year = targetYear,
            TotalRequests = requests.Count,
            DraftCount = requests.Count(x => x.Status == RequestStatus.Draft),
            SubmittedCount = requests.Count(x => x.Status == RequestStatus.Submitted),
            ApprovedCount = requests.Count(x => x.Status == RequestStatus.Approved),
            RejectedCount = requests.Count(x => x.Status == RequestStatus.Rejected),
            TotalAmountRequested = totalAmountRequested,
            TotalAmountDisbursed = totalAmountDisbursed,
            LeaveDaysTotal = totalLeaveDays,
            BreakdownByType = typeBreakdowns
        };
    }

    private static string GetStatusName(RequestStatus status)
    {
        return status switch
        {
            RequestStatus.Draft => "Nháp",
            RequestStatus.Submitted => "Chờ duyệt",
            RequestStatus.Approved => "Đã duyệt",
            RequestStatus.Rejected => "Từ chối",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Lấy báo cáo thống kê ngày nghỉ phép của nhân viên theo năm và phòng ban có phân trang, tìm kiếm.
    /// </summary>
    public virtual async Task<PagedResultDto<EmployeeLeaveReportItemDto>> GetLeaveReportAsync(GetLeaveReportInput input)
    {
        var targetYear = input.Year ?? DateTime.UtcNow.Year;
        var scope = await EvaluateCurrentUserAccessScopeAsync();

        // Nếu là Manager và chọn lọc theo phòng ban không thuộc quyền quản lý -> throw AbpAuthorizationException
        if (scope.IsManager && input.DepartmentId.HasValue && !scope.ManagedDeptIds.Contains(input.DepartmentId.Value))
        {
            throw new AbpAuthorizationException("Bạn không có quyền truy cập dữ liệu của phòng ban này.");
        }

        var users = await _userRepository.GetListAsync();
        var members = await _memberRepository.GetListAsync();
        var departments = await _departmentRepository.GetListAsync();

        var memberDict = members.ToDictionary(x => x.UserId, x => x);
        var deptDict = departments.ToDictionary(x => x.Id, x => x);

        var requestQuery = await _requestRepository.GetQueryableAsync();
        var allLeaveRequests = await AsyncExecuter.ToListAsync(
            requestQuery.Where(x => x.Type == RequestType.Leave && x.CreatorId.HasValue));

        var userLeavesDict = allLeaveRequests
            .GroupBy(x => x.CreatorId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var windowStart = new DateTime(targetYear, 1, 1);
        var windowEnd = new DateTime(targetYear, 12, 31, 23, 59, 59);

        var reportItems = new List<EmployeeLeaveReportItemDto>();

        foreach (var user in users)
        {
            memberDict.TryGetValue(user.Id, out var member);
            Department? dept = null;
            if (member != null && deptDict.TryGetValue(member.DepartmentId, out var d))
            {
                dept = d;
            }

            // Kiểm tra phân quyền truy cập theo phân cấp
            if (!scope.IsGlobal)
            {
                if (scope.IsManager)
                {
                    var isManagedDeptMember = member != null && scope.ManagedDeptIds.Contains(member.DepartmentId);
                    var isSelf = user.Id == scope.CurrentUserId;
                    if (!isManagedDeptMember && !isSelf)
                    {
                        continue;
                    }
                }
                else
                {
                    if (user.Id != scope.CurrentUserId)
                    {
                        continue;
                    }
                }
            }

            // Lọc theo phòng ban cụ thể nếu có
            if (input.DepartmentId.HasValue && (member == null || member.DepartmentId != input.DepartmentId.Value))
            {
                continue;
            }

            var fullName = (!string.IsNullOrWhiteSpace(user.Surname) ? user.Surname + " " : "") + user.Name;
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = user.UserName;
            }

            var item = new EmployeeLeaveReportItemDto
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = fullName.Trim(),
                Email = user.Email,
                DepartmentId = member?.DepartmentId,
                DepartmentCode = dept?.Code,
                DepartmentName = dept?.Name,
                Position = member?.Position ?? string.Empty
            };

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                var f = input.Filter.Trim().ToLowerInvariant();
                var match = item.FullName.ToLowerInvariant().Contains(f)
                    || item.UserName.ToLowerInvariant().Contains(f)
                    || item.Email.ToLowerInvariant().Contains(f)
                    || item.Position.ToLowerInvariant().Contains(f)
                    || (item.DepartmentName != null && item.DepartmentName.ToLowerInvariant().Contains(f));

                if (!match)
                {
                    continue;
                }
            }

            // Tính toán số ngày nghỉ phép
            userLeavesDict.TryGetValue(user.Id, out var userLeaves);
            userLeaves ??= new List<Request>();

            double totalApprovedLeaveDays = 0;
            int totalSentLeaveRequests = 0;
            int totalApprovedLeaveRequests = 0;

            var inYearApprovedLeaves = new List<(Request Request, LeaveData Extracted)>();
            var inYearSentLeaves = new List<(Request Request, LeaveData Extracted)>();

            foreach (var req in userLeaves)
            {
                var extracted = LeaveTimelineExtractor.Extract(req);
                bool inYear = (extracted.StartDate <= windowEnd && extracted.EndDate >= windowStart)
                              || (req.CreationTime.Year == targetYear);

                if (inYear)
                {
                    if (req.Status != RequestStatus.Draft)
                    {
                        totalSentLeaveRequests++;
                        inYearSentLeaves.Add((req, extracted));
                    }

                    if (req.Status == RequestStatus.Approved)
                    {
                        totalApprovedLeaveRequests++;
                        inYearApprovedLeaves.Add((req, extracted));

                        // Tính số ngày nghỉ nằm trong targetYear
                        double daysInYear;
                        if (extracted.StartDate >= windowStart && extracted.EndDate <= windowEnd)
                        {
                            daysInYear = extracted.Days;
                        }
                        else
                        {
                            var effectiveStart = extracted.StartDate < windowStart ? windowStart : extracted.StartDate;
                            var effectiveEnd = extracted.EndDate > windowEnd ? windowEnd : extracted.EndDate;
                            if (effectiveEnd >= effectiveStart)
                            {
                                daysInYear = (effectiveEnd.Date - effectiveStart.Date).TotalDays + 1;
                            }
                            else
                            {
                                daysInYear = 0;
                            }
                        }

                        totalApprovedLeaveDays += daysInYear;
                    }
                }
            }

            item.TotalApprovedLeaveDays = totalApprovedLeaveDays;
            item.TotalSentLeaveRequests = totalSentLeaveRequests;
            item.TotalApprovedLeaveRequests = totalApprovedLeaveRequests;

            // Xác định đợt nghỉ phép gần nhất
            var latestCandidate = inYearApprovedLeaves
                .OrderByDescending(x => x.Extracted.StartDate)
                .ThenByDescending(x => x.Request.CreationTime)
                .Select(x => x.Extracted)
                .FirstOrDefault();

            if (latestCandidate == null)
            {
                latestCandidate = inYearSentLeaves
                    .OrderByDescending(x => x.Extracted.StartDate)
                    .ThenByDescending(x => x.Request.CreationTime)
                    .Select(x => x.Extracted)
                    .FirstOrDefault();
            }

            if (latestCandidate != null)
            {
                item.LatestLeaveStartDate = latestCandidate.StartDate;
                item.LatestLeaveEndDate = latestCandidate.EndDate;
                item.LatestLeaveDays = latestCandidate.Days;
                if (latestCandidate.StartDate.Date == latestCandidate.EndDate.Date)
                {
                    item.LatestLeavePeriodFormatted = $"{latestCandidate.StartDate:dd/MM/yyyy} ({latestCandidate.Days} ngày)";
                }
                else
                {
                    item.LatestLeavePeriodFormatted = $"{latestCandidate.StartDate:dd/MM/yyyy} - {latestCandidate.EndDate:dd/MM/yyyy} ({latestCandidate.Days} ngày)";
                }
            }
            else
            {
                item.LatestLeaveStartDate = null;
                item.LatestLeaveEndDate = null;
                item.LatestLeaveDays = null;
                item.LatestLeavePeriodFormatted = "Chưa có";
            }

            reportItems.Add(item);
        }

        var totalCount = reportItems.Count;

        IEnumerable<EmployeeLeaveReportItemDto> ordered = reportItems;
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            try
            {
                ordered = reportItems.AsQueryable().OrderBy(input.Sorting);
            }
            catch
            {
                ordered = reportItems.OrderByDescending(x => x.TotalApprovedLeaveDays).ThenBy(x => x.FullName);
            }
        }
        else
        {
            ordered = reportItems.OrderByDescending(x => x.TotalApprovedLeaveDays).ThenBy(x => x.FullName);
        }

        var pagedItems = ordered
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return new PagedResultDto<EmployeeLeaveReportItemDto>(totalCount, pagedItems);
    }
}
