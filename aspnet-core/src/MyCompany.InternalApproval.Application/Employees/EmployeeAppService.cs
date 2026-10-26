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
using Volo.Abp.Validation;

namespace MyCompany.InternalApproval.Employees;

// Dịch vụ quản lý Hồ sơ Nhân viên, Thâm niên làm việc, Timeline nghỉ phép và Thống kê nhân sự
[Authorize]
[RemoteService(IsEnabled = false)]
public class EmployeeAppService : InternalApprovalAppService, IEmployeeAppService
{
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IRepository<DepartmentMember, Guid> _memberRepository;
    private readonly IRequestRepository _requestRepository;
    private readonly IPermissionManager _permissionManager;

    public EmployeeAppService(
        IRepository<IdentityUser, Guid> userRepository,
        IDepartmentRepository departmentRepository,
        IRepository<DepartmentMember, Guid> memberRepository,
        IRequestRepository requestRepository,
        IPermissionManager permissionManager)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _memberRepository = memberRepository;
        _requestRepository = requestRepository;
        _permissionManager = permissionManager;
    }

    // Đánh giá phạm vi quyền hạn dữ liệu của người dùng hiện tại (Admin / Trưởng phòng / Nhân viên)
    protected virtual async Task<EmployeeAccessScope> EvaluateCurrentUserAccessScopeAsync()
    {
        var currentUserId = CurrentUser.Id;
        var isGlobal = CurrentUser.IsInRole("admin")
            || (CurrentUser.UserName != null && CurrentUser.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase));

        if (!isGlobal && currentUserId.HasValue)
        {
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
                var roleGrants = await Task.WhenAll(
                    CurrentUser.Roles.Select(role =>
                        _permissionManager.GetAsync(
                            InternalApprovalPermissions.Employees.ViewAll,
                            RolePermissionValueProvider.ProviderName,
                            role))
                );
                isGlobal = roleGrants.Any(g => g?.IsGranted == true);
            }
        }

        if (isGlobal)
        {
            return new EmployeeAccessScope(EmployeeAccessLevel.Global, currentUserId);
        }

        if (currentUserId.HasValue)
        {
            var managedDepts = await _departmentRepository.GetListAsync(d => d.ManagerId == currentUserId.Value);
            var managedDeptIds = managedDepts.Select(d => d.Id).ToHashSet();

            if (managedDeptIds.Count > 0)
            {
                return new EmployeeAccessScope(EmployeeAccessLevel.DepartmentManager, currentUserId, managedDeptIds);
            }
        }

        return new EmployeeAccessScope(EmployeeAccessLevel.SelfOnly, currentUserId);
    }

    // Kiểm tra quyền truy cập thông tin của một nhân sự mục tiêu theo cấu trúc phân quyền phân cấp
    protected virtual async Task CheckCanAccessEmployeeAsync(Guid targetUserId)
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            throw new AbpAuthorizationException("Người dùng chưa đăng nhập.");
        }

        if (targetUserId == currentUserId.Value)
        {
            return;
        }

        var scope = await EvaluateCurrentUserAccessScopeAsync();

        if (scope.IsGlobal)
        {
            return;
        }

        if (scope.IsManager)
        {
            var targetMember = await _memberRepository.FirstOrDefaultAsync(m => m.UserId == targetUserId);
            if (targetMember != null && scope.ManagedDeptIds.Contains(targetMember.DepartmentId))
            {
                return;
            }
        }

        throw new AbpAuthorizationException("Bạn không có quyền truy cập thông tin của nhân sự này.");
    }

    // Lấy danh sách nhân viên có phân trang, tìm kiếm họ tên/email/vị trí và lọc theo phòng ban, thâm niên
    public virtual async Task<PagedResultDto<EmployeeListItemDto>> GetListAsync(GetEmployeeListInput input)
    {
        try
        {
            var scope = await EvaluateCurrentUserAccessScopeAsync();

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

            var memberDict = members
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.First());
            var deptDict = departments.ToDictionary(x => x.Id, x => x);

            var filterKeyword = !string.IsNullOrWhiteSpace(input.Filter)
                ? input.Filter.Trim().ToLowerInvariant()
                : null;

            var employeeList = users
                .Where(user =>
                {
                    if (scope.IsGlobal) return true;
                    memberDict.TryGetValue(user.Id, out var member);
                    if (scope.IsManager)
                    {
                        var isManagedDeptMember = member != null && scope.ManagedDeptIds.Contains(member.DepartmentId);
                        return isManagedDeptMember || user.Id == scope.CurrentUserId;
                    }
                    return user.Id == scope.CurrentUserId;
                })
                .Select(user =>
                {
                    memberDict.TryGetValue(user.Id, out var member);
                    Department? dept = null;
                    if (member != null && deptDict.TryGetValue(member.DepartmentId, out var d))
                    {
                        dept = d;
                    }

                    var rawJoinDate = member?.JoinDate ?? TenureCalculator.TryGetJoinDate(user.ExtraProperties);
                    var effectiveJoinDate = TenureCalculator.GetEffectiveJoinDate(rawJoinDate, user.CreationTime);
                    var (years, months, totalMonths) = TenureCalculator.Calculate(effectiveJoinDate);
                    var tenureFormatted = TenureCalculator.FormatVietnamese(years, months);

                    var fullName = (!string.IsNullOrWhiteSpace(user.Surname) ? user.Surname + " " : "") + user.Name;
                    if (string.IsNullOrWhiteSpace(fullName))
                    {
                        fullName = user.UserName;
                    }

                    requestCountDict.TryGetValue(user.Id, out var reqCount);

                    return new EmployeeListItemDto
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
                })
                .Where(item => !input.DepartmentId.HasValue || item.DepartmentId == input.DepartmentId.Value)
                .Where(item => string.IsNullOrEmpty(filterKeyword)
                    || item.FullName.ToLowerInvariant().Contains(filterKeyword)
                    || item.UserName.ToLowerInvariant().Contains(filterKeyword)
                    || item.Email.ToLowerInvariant().Contains(filterKeyword)
                    || item.Position.ToLowerInvariant().Contains(filterKeyword)
                    || (item.DepartmentName != null && item.DepartmentName.ToLowerInvariant().Contains(filterKeyword)))
                .Where(item => !input.MinTenureMonths.HasValue || item.TenureMonths >= input.MinTenureMonths.Value)
                .Where(item => !input.MaxTenureMonths.HasValue || item.TenureMonths <= input.MaxTenureMonths.Value)
                .ToList();

            var totalCount = employeeList.Count;

            var pagedItems = employeeList
                .OrderByDescending(x => x.IsManager)
                .ThenBy(x => x.FullName)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();

            return new PagedResultDto<EmployeeListItemDto>(totalCount, pagedItems);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetListAsync nhân viên: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Lấy chi tiết hồ sơ nhân viên, thâm niên làm việc, thống kê trong năm và các đề xuất gần nhất
    public virtual async Task<EmployeeProfileDto> GetAsync(Guid id)
    {
        try
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

            var rawJoinDate = member?.JoinDate ?? TenureCalculator.TryGetJoinDate(user.ExtraProperties);
            var effectiveJoinDate = TenureCalculator.GetEffectiveJoinDate(rawJoinDate, user.CreationTime);
            var (years, months, totalMonths) = TenureCalculator.Calculate(effectiveJoinDate);
            var tenureFormatted = TenureCalculator.FormatVietnamese(years, months);

            var fullName = (!string.IsNullOrWhiteSpace(user.Surname) ? user.Surname + " " : "") + user.Name;
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = user.UserName;
            }
            var trimmedFullName = fullName.Trim();

            var currentYear = DateTime.UtcNow.Year;
            var yearlyStats = await CalculateYearlyStatsAsync(id, currentYear);

            var requestQuery = await _requestRepository.GetQueryableAsync();
            var recentEntities = await AsyncExecuter.ToListAsync(
                requestQuery
                    .Where(x => x.CreatorId == id)
                    .OrderByDescending(x => x.CreationTime)
                    .Take(10));

            var recentDtos = MapWithExceptionHandling<List<Request>, List<RequestDto>>(recentEntities)
                .Select(rDto =>
                {
                    rDto.CreatorName = trimmedFullName;
                    rDto.DepartmentName = dept?.Name;
                    rDto.DepartmentCode = dept?.Code;
                    return rDto;
                }).ToList();

            return new EmployeeProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = trimmedFullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DepartmentId = member?.DepartmentId,
                DepartmentCode = dept?.Code,
                DepartmentName = dept?.Name,
                Position = member?.Position ?? string.Empty,
                IsManager = dept != null && dept.ManagerId == user.Id,
                JoinDate = effectiveJoinDate,
                RawJoinDate = rawJoinDate,
                CreationTime = user.CreationTime,
                TenureMonths = totalMonths,
                TenureFormatted = tenureFormatted,
                CurrentYearRequestCount = yearlyStats.TotalRequests,
                CurrentYearStats = yearlyStats,
                RecentRequests = recentDtos
            };
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetAsync nhân viên {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Cập nhật ngày bắt đầu làm việc của nhân viên và tính toán lại thâm niên
    [Authorize(InternalApprovalPermissions.Employees.Edit)]
    public virtual async Task<EmployeeProfileDto> UpdateJoinDateAsync(Guid id, UpdateEmployeeJoinDateDto input)
    {
        try
        {
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
                member.SetJoinDate(input.JoinDate.Date);
                await _memberRepository.UpdateAsync(member, autoSave: true);
            }
            else
            {
                user.ExtraProperties["JoinDate"] = input.JoinDate.Date;
                await _userRepository.UpdateAsync(user, autoSave: true);
            }

            return await GetAsync(id);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong UpdateJoinDateAsync nhân viên {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Lấy lịch sử đề xuất của nhân viên kèm bộ lọc theo trạng thái, loại hình và năm
    public virtual async Task<PagedResultDto<RequestDto>> GetRequestsAsync(Guid id, GetEmployeeRequestsInput input)
    {
        try
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

            var approverIds = requests.Where(x => x.ApproverId.HasValue).Select(x => x.ApproverId!.Value).Distinct().ToList();
            var approvers = approverIds.Any()
                ? await _userRepository.GetListAsync(x => approverIds.Contains(x.Id))
                : new List<IdentityUser>();
            var approverDict = approvers.ToDictionary(
                x => x.Id,
                x => (!string.IsNullOrWhiteSpace(x.Surname) ? x.Surname + " " : "") + x.Name);

            var dtos = MapWithExceptionHandling<List<Request>, List<RequestDto>>(requests)
                .Select(dto =>
                {
                    dto.CreatorName = fullName;
                    dto.DepartmentName = dept?.Name;
                    dto.DepartmentCode = dept?.Code;

                    if (dto.ApproverId.HasValue && approverDict.TryGetValue(dto.ApproverId.Value, out var approverName))
                    {
                        dto.ApproverName = approverName;
                    }
                    return dto;
                }).ToList();

            return new PagedResultDto<RequestDto>(totalCount, dtos);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetRequestsAsync nhân viên {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Thống kê đề xuất theo năm của nhân viên
    public virtual async Task<EmployeeYearlyStatsDto> GetYearlyStatsAsync(Guid id, int? year = null)
    {
        try
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
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetYearlyStatsAsync nhân viên {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Lấy danh sách các đợt nghỉ phép phục vụ timeline nghỉ phép trực quan
    public virtual async Task<List<LeaveTimelineItemDto>> GetLeaveTimelineAsync(Guid id, int? year = null)
    {
        try
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

            var approverIds = leaves.Where(x => x.ApproverId.HasValue).Select(x => x.ApproverId!.Value).Distinct().ToList();
            var approvers = approverIds.Any()
                ? await _userRepository.GetListAsync(x => approverIds.Contains(x.Id))
                : new List<IdentityUser>();
            var approverDict = approvers.ToDictionary(
                x => x.Id,
                x => (!string.IsNullOrWhiteSpace(x.Surname) ? x.Surname + " " : "") + x.Name);

            return leaves
                .Select(req => (Request: req, Extracted: LeaveTimelineExtractor.Extract(req)))
                .Where(x => !year.HasValue || x.Extracted.StartDate.Year == year.Value || x.Request.CreationTime.Year == year.Value)
                .Select(x =>
                {
                    approverDict.TryGetValue(x.Request.ApproverId ?? Guid.Empty, out var approverName);

                    return new LeaveTimelineItemDto
                    {
                        RequestId = x.Request.Id,
                        Title = x.Request.Title,
                        StartDate = x.Extracted.StartDate,
                        EndDate = x.Extracted.EndDate,
                        Days = x.Extracted.Days,
                        Reason = x.Extracted.Reason,
                        Status = x.Request.Status,
                        StatusName = GetStatusName(x.Request.Status),
                        CreationTime = x.Request.CreationTime,
                        ApproverName = approverName,
                        ApprovalTime = x.Request.ApprovalTime,
                        RejectionReason = x.Request.RejectionReason
                    };
                })
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.CreationTime)
                .ToList();
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetLeaveTimelineAsync nhân viên {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Lấy dữ liệu phân tích và thống kê tổng thể nhân sự
    [Authorize]
    public virtual async Task<HrAnalyticsDto> GetAnalyticsAsync(int? year = null)
    {
        try
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

            var memberDict = members
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.First());
            var deptDict = departments.ToDictionary(x => x.Id, x => x);

            // 1. Thống kê tổng quan công ty
            var assignedDeptIds = departments.Select(d => d.Id).ToHashSet();
            var assignedMembers = members.Where(m => assignedDeptIds.Contains(m.DepartmentId)).ToList();
            var assignedUserIds = assignedMembers.Select(x => x.UserId).ToHashSet();
            var assignedUsers = users.Where(u => assignedUserIds.Contains(u.Id)).ToList();
            var totalEmployees = assignedUsers.Count;
            var totalDepartments = departments.Count;

            var tenureMonthsList = assignedUsers.Select(u =>
            {
                memberDict.TryGetValue(u.Id, out var m);
                var effectiveJoin = TenureCalculator.GetEffectiveJoinDate(m?.JoinDate, u.CreationTime);
                return TenureCalculator.Calculate(effectiveJoin).TotalMonths;
            }).ToList();

            var avgCompanyTenureMonths = tenureMonthsList.Any() ? tenureMonthsList.Average() : 0.0;
            var totalRequestsThisYear = allRequests.Count;
            var totalDisbursedThisYear = allRequests
                .Where(x => x.Status == RequestStatus.Approved && x.Amount.HasValue)
                .Sum(x => x.Amount!.Value);

            // 2. Phân bổ quân số, thâm niên và tần suất đề xuất theo phòng ban
            var userDeptLookup = memberDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DepartmentId);
            var requestsByDept = allRequests
                .Where(x => x.CreatorId.HasValue && userDeptLookup.ContainsKey(x.CreatorId!.Value))
                .GroupBy(x => userDeptLookup[x.CreatorId!.Value])
                .ToDictionary(g => g.Key, g => g.ToList());

            var deptStats = departments.Select(dept =>
            {
                var memberUserIds = members
                    .Where(x => x.DepartmentId == dept.Id)
                    .Select(x => x.UserId)
                    .ToHashSet();
                var deptUsers = users.Where(x => memberUserIds.Contains(x.Id)).ToList();

                var headcount = deptUsers.Count;
                var percentage = totalEmployees > 0 ? Math.Round((double)headcount / totalEmployees * 100, 1) : 0;

                var deptTenures = deptUsers.Select(u =>
                {
                    memberDict.TryGetValue(u.Id, out var m);
                    var effectiveJoin = TenureCalculator.GetEffectiveJoinDate(m?.JoinDate, u.CreationTime);
                    return TenureCalculator.Calculate(effectiveJoin).TotalMonths;
                }).ToList();

                var avgDeptTenure = deptTenures.Any() ? deptTenures.Average() : 0.0;
                var minTenure = deptTenures.Any() ? deptTenures.Min() : 0;
                var maxTenure = deptTenures.Any() ? deptTenures.Max() : 0;

                requestsByDept.TryGetValue(dept.Id, out var deptRequests);
                var reqCount = deptRequests?.Count ?? 0;
                var reqPerEmp = headcount > 0 ? Math.Round((double)reqCount / headcount, 2) : 0;

                return (
                    Headcount: new DepartmentHeadcountDto
                    {
                        DepartmentId = dept.Id,
                        DepartmentCode = dept.Code,
                        DepartmentName = dept.Name,
                        Headcount = headcount,
                        Percentage = percentage
                    },
                    Tenure: new DepartmentTenureDto
                    {
                        DepartmentId = dept.Id,
                        DepartmentCode = dept.Code,
                        DepartmentName = dept.Name,
                        AverageTenureMonths = Math.Round(avgDeptTenure, 1),
                        AverageTenureFormatted = TenureCalculator.FormatAverageMonths(avgDeptTenure),
                        MinTenureMonths = minTenure,
                        MaxTenureMonths = maxTenure
                    },
                    Frequency: new DepartmentRequestFrequencyDto
                    {
                        DepartmentId = dept.Id,
                        DepartmentCode = dept.Code,
                        DepartmentName = dept.Name,
                        TotalRequests = reqCount,
                        RequestsPerEmployee = reqPerEmp
                    }
                );
            }).ToList();

            // 3. Danh sách nhân sự gửi nhiều đề xuất nhất
            var userDict = users.ToDictionary(x => x.Id, x => x);
            var topRequesters = allRequests
                .Where(x => x.CreatorId.HasValue)
                .GroupBy(x => x.CreatorId!.Value)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Where(g => userDict.ContainsKey(g.Key))
                .Select(g =>
                {
                    var u = userDict[g.Key];
                    memberDict.TryGetValue(u.Id, out var m);
                    Department? d = null;
                    if (m != null && deptDict.TryGetValue(m.DepartmentId, out var foundDept))
                    {
                        d = foundDept;
                    }

                    var uName = (!string.IsNullOrWhiteSpace(u.Surname) ? u.Surname + " " : "") + u.Name;
                    return new TopRequesterDto
                    {
                        UserId = u.Id,
                        FullName = !string.IsNullOrWhiteSpace(uName) ? uName.Trim() : u.UserName,
                        DepartmentName = d?.Name ?? "N/A",
                        RequestCount = g.Count()
                    };
                }).ToList();

            return new HrAnalyticsDto
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
                },
                HeadcountByDepartment = deptStats.Select(x => x.Headcount).ToList(),
                TenureByDepartment = deptStats.Select(x => x.Tenure).ToList(),
                RequestFrequencyByDepartment = deptStats.Select(x => x.Frequency).ToList(),
                TopRequesters = topRequesters
            };
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetAnalyticsAsync nhân sự: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Tính toán thống kê đề xuất theo năm của một nhân viên
    private async Task<EmployeeYearlyStatsDto> CalculateYearlyStatsAsync(Guid userId, int targetYear)
    {
        var requestQuery = await _requestRepository.GetQueryableAsync();
        var requests = await AsyncExecuter.ToListAsync(
            requestQuery.Where(x => x.CreatorId == userId && x.CreationTime.Year == targetYear));

        var leaveRequests = requests.Where(x => x.Type == RequestType.Leave).ToList();
        var totalLeaveDays = leaveRequests
            .Where(x => x.Status == RequestStatus.Approved)
            .Sum(lr => LeaveTimelineExtractor.Extract(lr).Days);

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

    // Chuyển đổi enum RequestStatus sang tên hiển thị tiếng Việt
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

    // Lấy báo cáo thống kê ngày nghỉ phép của nhân viên theo năm và phòng ban có phân trang, tìm kiếm
    public virtual async Task<PagedResultDto<EmployeeLeaveReportItemDto>> GetLeaveReportAsync(GetLeaveReportInput input)
    {
        try
        {
            var targetYear = input.Year ?? DateTime.UtcNow.Year;
            var scope = await EvaluateCurrentUserAccessScopeAsync();

            if (scope.IsManager && input.DepartmentId.HasValue && !scope.ManagedDeptIds.Contains(input.DepartmentId.Value))
            {
                throw new AbpAuthorizationException("Bạn không có quyền truy cập dữ liệu của phòng ban này.");
            }

            var users = await _userRepository.GetListAsync();
            var members = await _memberRepository.GetListAsync();
            var departments = await _departmentRepository.GetListAsync();

            var memberDict = members
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.First());
            var deptDict = departments.ToDictionary(x => x.Id, x => x);

            var requestQuery = await _requestRepository.GetQueryableAsync();
            var allLeaveRequests = await AsyncExecuter.ToListAsync(
                requestQuery.Where(x => x.Type == RequestType.Leave && x.CreatorId.HasValue));

            var userLeavesDict = allLeaveRequests
                .GroupBy(x => x.CreatorId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var windowStart = new DateTime(targetYear, 1, 1);
            var windowEnd = new DateTime(targetYear, 12, 31, 23, 59, 59);

            var filterKeyword = !string.IsNullOrWhiteSpace(input.Filter)
                ? input.Filter.Trim().ToLowerInvariant()
                : null;

            var reportItems = users
                .Where(user =>
                {
                    if (scope.IsGlobal) return true;
                    memberDict.TryGetValue(user.Id, out var member);
                    if (scope.IsManager)
                    {
                        var isManagedDeptMember = member != null && scope.ManagedDeptIds.Contains(member.DepartmentId);
                        return isManagedDeptMember || user.Id == scope.CurrentUserId;
                    }
                    return user.Id == scope.CurrentUserId;
                })
                .Where(user =>
                {
                    if (!input.DepartmentId.HasValue) return true;
                    memberDict.TryGetValue(user.Id, out var member);
                    return member != null && member.DepartmentId == input.DepartmentId.Value;
                })
                .Select(user =>
                {
                    memberDict.TryGetValue(user.Id, out var member);
                    Department? dept = null;
                    if (member != null && deptDict.TryGetValue(member.DepartmentId, out var d))
                    {
                        dept = d;
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

                    userLeavesDict.TryGetValue(user.Id, out var userLeaves);
                    userLeaves ??= new List<Request>();

                    var extractedLeaves = userLeaves
                        .Select(req => (Request: req, Extracted: LeaveTimelineExtractor.Extract(req)))
                        .Where(x => (x.Extracted.StartDate <= windowEnd && x.Extracted.EndDate >= windowStart)
                                    || x.Request.CreationTime.Year == targetYear)
                        .ToList();

                    var inYearSentLeaves = extractedLeaves
                        .Where(x => x.Request.Status != RequestStatus.Draft)
                        .ToList();

                    var inYearApprovedLeaves = extractedLeaves
                        .Where(x => x.Request.Status == RequestStatus.Approved)
                        .ToList();

                    var totalApprovedLeaveDays = inYearApprovedLeaves.Sum(x =>
                    {
                        if (x.Extracted.StartDate >= windowStart && x.Extracted.EndDate <= windowEnd)
                        {
                            return x.Extracted.Days;
                        }

                        var effectiveStart = x.Extracted.StartDate < windowStart ? windowStart : x.Extracted.StartDate;
                        var effectiveEnd = x.Extracted.EndDate > windowEnd ? windowEnd : x.Extracted.EndDate;
                        return effectiveEnd >= effectiveStart
                            ? (effectiveEnd.Date - effectiveStart.Date).TotalDays + 1
                            : 0.0;
                    });

                    item.TotalApprovedLeaveDays = totalApprovedLeaveDays;
                    item.TotalSentLeaveRequests = inYearSentLeaves.Count;
                    item.TotalApprovedLeaveRequests = inYearApprovedLeaves.Count;

                    var latestCandidate = inYearApprovedLeaves
                        .OrderByDescending(x => x.Extracted.StartDate)
                        .ThenByDescending(x => x.Request.CreationTime)
                        .Select(x => x.Extracted)
                        .FirstOrDefault()
                        ?? inYearSentLeaves
                            .OrderByDescending(x => x.Extracted.StartDate)
                            .ThenByDescending(x => x.Request.CreationTime)
                            .Select(x => x.Extracted)
                            .FirstOrDefault();

                    if (latestCandidate != null)
                    {
                        item.LatestLeaveStartDate = latestCandidate.StartDate;
                        item.LatestLeaveEndDate = latestCandidate.EndDate;
                        item.LatestLeaveDays = latestCandidate.Days;
                        item.LatestLeavePeriodFormatted = latestCandidate.StartDate.Date == latestCandidate.EndDate.Date
                            ? $"{latestCandidate.StartDate:dd/MM/yyyy} ({latestCandidate.Days} ngày)"
                            : $"{latestCandidate.StartDate:dd/MM/yyyy} - {latestCandidate.EndDate:dd/MM/yyyy} ({latestCandidate.Days} ngày)";
                    }
                    else
                    {
                        item.LatestLeaveStartDate = null;
                        item.LatestLeaveEndDate = null;
                        item.LatestLeaveDays = null;
                        item.LatestLeavePeriodFormatted = "Chưa có";
                    }

                    return item;
                })
                .Where(item => string.IsNullOrEmpty(filterKeyword)
                    || item.FullName.ToLowerInvariant().Contains(filterKeyword)
                    || item.UserName.ToLowerInvariant().Contains(filterKeyword)
                    || item.Email.ToLowerInvariant().Contains(filterKeyword)
                    || item.Position.ToLowerInvariant().Contains(filterKeyword)
                    || (item.DepartmentName != null && item.DepartmentName.ToLowerInvariant().Contains(filterKeyword)))
                .ToList();

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
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetLeaveReportAsync: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }
}
