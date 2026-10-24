using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MyCompany.InternalApproval.Employees;
using MyCompany.InternalApproval.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace MyCompany.InternalApproval.Departments;

// Dịch vụ quản lý phòng ban và thành viên
[Authorize]
public class DepartmentAppService : InternalApprovalAppService, IDepartmentAppService
{
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<DepartmentMember, Guid> _memberRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IdentityUserManager _userManager;

    public DepartmentAppService(
        IRepository<Department, Guid> departmentRepository,
        IRepository<DepartmentMember, Guid> memberRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IdentityUserManager userManager)
    {
        _departmentRepository = departmentRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _userManager = userManager;
    }

    // Lấy danh sách phòng ban có phân trang
    public virtual async Task<PagedResultDto<DepartmentDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        try
        {
            var deptQuery = await _departmentRepository.GetQueryableAsync();
            var memberQuery = await _memberRepository.GetQueryableAsync();
            var userQuery = await _userRepository.GetQueryableAsync();

            var totalCount = await AsyncExecuter.CountAsync(deptQuery);

            var sorting = !string.IsNullOrWhiteSpace(input.Sorting)
                ? input.Sorting
                : $"{nameof(Department.CreationTime)} desc";

            var pagedDepts = await AsyncExecuter.ToListAsync(
                deptQuery
                    .OrderBy(sorting)
                    .Skip(input.SkipCount)
                    .Take(input.MaxResultCount)
            );

            var deptIds = pagedDepts.Select(x => x.Id).ToList();
            var managerIds = pagedDepts
                .Where(x => x.ManagerId.HasValue)
                .Select(x => x.ManagerId!.Value)
                .Distinct()
                .ToList();

            var memberCountList = await AsyncExecuter.ToListAsync(
                memberQuery.Where(x => deptIds.Contains(x.DepartmentId))
            );
            var memberCountDict = memberCountList
                .GroupBy(x => x.DepartmentId)
                .ToDictionary(g => g.Key, g => g.Count());

            var managerList = await AsyncExecuter.ToListAsync(
                userQuery.Where(x => managerIds.Contains(x.Id))
            );
            var managerDict = managerList.ToDictionary(
                x => x.Id,
                x => (!string.IsNullOrWhiteSpace(x.Surname) ? x.Surname + " " : "") + x.Name
            );

            var dtos = pagedDepts.Select(d =>
            {
                memberCountDict.TryGetValue(d.Id, out var count);
                string? mgrName = null;
                if (d.ManagerId.HasValue)
                {
                    managerDict.TryGetValue(d.ManagerId.Value, out mgrName);
                }

                return new DepartmentDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Description = d.Description,
                    ManagerId = d.ManagerId,
                    ManagerName = mgrName,
                    MemberCount = count,
                    CreationTime = d.CreationTime
                };
            }).ToList();

            return new PagedResultDto<DepartmentDto>(totalCount, dtos);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetListAsync phòng ban: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Lấy chi tiết một phòng ban theo Id
    [Authorize(InternalApprovalPermissions.Departments.Default)]
    public virtual async Task<DepartmentDto> GetAsync(Guid id)
    {
        try
        {
            var dept = await _departmentRepository.GetAsync(id);
            var memberQuery = await _memberRepository.GetQueryableAsync();
            var memberCount = await AsyncExecuter.CountAsync(memberQuery.Where(x => x.DepartmentId == id));

            string? managerName = null;
            if (dept.ManagerId.HasValue)
            {
                var manager = await _userRepository.FindAsync(dept.ManagerId.Value);
                if (manager != null)
                {
                    managerName = (!string.IsNullOrWhiteSpace(manager.Surname) ? manager.Surname + " " : "") + manager.Name;
                }
            }

            return new DepartmentDto
            {
                Id = dept.Id,
                Code = dept.Code,
                Name = dept.Name,
                Description = dept.Description,
                ManagerId = dept.ManagerId,
                ManagerName = managerName,
                MemberCount = memberCount,
                CreationTime = dept.CreationTime
            };
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetAsync phòng ban {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Tạo mới phòng ban
    [Authorize(InternalApprovalPermissions.Departments.Create)]
    public virtual async Task<DepartmentDto> CreateAsync(CreateUpdateDepartmentDto input)
    {
        try
        {
            var existing = await _departmentRepository.FirstOrDefaultAsync(x => x.Code == input.Code);
            if (existing != null)
            {
                throw new UserFriendlyException($"Mã phòng ban '{input.Code}' đã tồn tại trong hệ thống.");
            }

            var dept = new Department(
                GuidGenerator.Create(),
                input.Code.Trim(),
                input.Name.Trim(),
                input.Description?.Trim(),
                input.ManagerId
            );

            await _departmentRepository.InsertAsync(dept, autoSave: true);

            // Nếu có chọn trưởng phòng, tự động thêm trưởng phòng vào danh sách thành viên với chức danh "Trưởng phòng"
            if (input.ManagerId.HasValue)
            {
                var member = new DepartmentMember(GuidGenerator.Create(), dept.Id, input.ManagerId.Value, "Trưởng phòng");
                await _memberRepository.InsertAsync(member, autoSave: true);
            }

            return await GetAsync(dept.Id);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong CreateAsync phòng ban: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Cập nhật thông tin phòng ban
    [Authorize(InternalApprovalPermissions.Departments.Edit)]
    public virtual async Task<DepartmentDto> UpdateAsync(Guid id, CreateUpdateDepartmentDto input)
    {
        try
        {
            var dept = await _departmentRepository.GetAsync(id);

            var existing = await _departmentRepository.FirstOrDefaultAsync(x => x.Code == input.Code && x.Id != id);
            if (existing != null)
            {
                throw new UserFriendlyException($"Mã phòng ban '{input.Code}' đã được sử dụng bởi phòng ban khác.");
            }

            dept.SetCode(input.Code.Trim());
            dept.SetName(input.Name.Trim());
            dept.SetDescription(input.Description?.Trim());
            dept.SetManager(input.ManagerId);

            await _departmentRepository.UpdateAsync(dept, autoSave: true);

            if (input.ManagerId.HasValue)
            {
                var existingMember = await _memberRepository.FirstOrDefaultAsync(x => x.DepartmentId == id && x.UserId == input.ManagerId.Value);
                if (existingMember == null)
                {
                    var member = new DepartmentMember(GuidGenerator.Create(), dept.Id, input.ManagerId.Value, "Trưởng phòng");
                    await _memberRepository.InsertAsync(member, autoSave: true);
                }
            }

            return await GetAsync(dept.Id);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong UpdateAsync phòng ban {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Xóa phòng ban
    [Authorize(InternalApprovalPermissions.Departments.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        try
        {
            var members = await _memberRepository.GetListAsync(x => x.DepartmentId == id);
            await _memberRepository.DeleteManyAsync(members, autoSave: true);
            await _departmentRepository.DeleteAsync(id, autoSave: true);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong DeleteAsync phòng ban {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Lấy danh sách thành viên trong phòng ban (dùng LINQ thay vì vòng lặp)
    public virtual async Task<List<DepartmentMemberDto>> GetMembersAsync(Guid departmentId)
    {
        try
        {
            var dept = await _departmentRepository.GetAsync(departmentId);
            var memberQuery = await _memberRepository.GetQueryableAsync();
            var userQuery = await _userRepository.GetQueryableAsync();

            var members = await AsyncExecuter.ToListAsync(memberQuery.Where(x => x.DepartmentId == departmentId));
            var userIds = members.Select(x => x.UserId).Distinct().ToList();

            var users = await AsyncExecuter.ToListAsync(userQuery.Where(x => userIds.Contains(x.Id)));
            var userDict = users.ToDictionary(x => x.Id, x => x);

            return members
                .Where(m => userDict.ContainsKey(m.UserId))
                .Select(m =>
                {
                    var u = userDict[m.UserId];
                    var effectiveJoinDate = TenureCalculator.GetEffectiveJoinDate(m.JoinDate, u.CreationTime);
                    var tenureFormatted = TenureCalculator.CalculateAndFormat(effectiveJoinDate);

                    return new DepartmentMemberDto
                    {
                        Id = m.Id,
                        DepartmentId = departmentId,
                        UserId = u.Id,
                        UserName = u.UserName,
                        FullName = (!string.IsNullOrWhiteSpace(u.Surname) ? u.Surname + " " : "") + u.Name,
                        Email = u.Email,
                        Position = m.Position,
                        IsManager = dept.ManagerId == u.Id,
                        JoinDate = m.JoinDate,
                        EffectiveJoinDate = effectiveJoinDate,
                        TenureFormatted = tenureFormatted,
                        CreationTime = m.CreationTime
                    };
                })
                .OrderByDescending(x => x.IsManager)
                .ThenBy(x => x.FullName)
                .ToList();
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetMembersAsync phòng ban {DepartmentId}: {Message}", departmentId, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Thêm nhân viên vào phòng ban
    [Authorize(InternalApprovalPermissions.Departments.ManageMembers)]
    public virtual async Task AddMemberAsync(Guid departmentId, AddDepartmentMemberDto input)
    {
        try
        {
            var dept = await _departmentRepository.GetAsync(departmentId);
            var existing = await _memberRepository.FirstOrDefaultAsync(x => x.DepartmentId == departmentId && x.UserId == input.UserId);
            if (existing != null)
            {
                existing.Position = input.Position.Trim();
                if (input.JoinDate.HasValue)
                {
                    existing.JoinDate = input.JoinDate.Value;
                }
                await _memberRepository.UpdateAsync(existing, autoSave: true);
                return;
            }

            var member = new DepartmentMember(GuidGenerator.Create(), departmentId, input.UserId, input.Position.Trim(), input.JoinDate);
            await _memberRepository.InsertAsync(member, autoSave: true);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong AddMemberAsync phòng ban {DepartmentId}: {Message}", departmentId, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Cập nhật chức danh hoặc ngày vào làm của thành viên trong phòng ban
    [Authorize(InternalApprovalPermissions.Departments.ManageMembers)]
    public virtual async Task UpdateMemberAsync(Guid departmentId, Guid userId, UpdateDepartmentMemberDto input)
    {
        try
        {
            var member = await _memberRepository.FirstOrDefaultAsync(x => x.DepartmentId == departmentId && x.UserId == userId);
            if (member == null)
            {
                throw new UserFriendlyException("Nhân viên không tồn tại trong phòng ban này.");
            }

            member.Position = input.Position.Trim();
            if (input.JoinDate.HasValue)
            {
                member.JoinDate = input.JoinDate.Value;
            }
            await _memberRepository.UpdateAsync(member, autoSave: true);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong UpdateMemberAsync phòng ban {DepartmentId}, User {UserId}: {Message}", departmentId, userId, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Gỡ thành viên khỏi phòng ban
    [Authorize(InternalApprovalPermissions.Departments.ManageMembers)]
    public virtual async Task RemoveMemberAsync(Guid departmentId, Guid userId)
    {
        try
        {
            var dept = await _departmentRepository.GetAsync(departmentId);
            if (dept.ManagerId == userId)
            {
                dept.SetManager(null);
                await _departmentRepository.UpdateAsync(dept, autoSave: true);
            }

            var member = await _memberRepository.FirstOrDefaultAsync(x => x.DepartmentId == departmentId && x.UserId == userId);
            if (member != null)
            {
                await _memberRepository.DeleteAsync(member, autoSave: true);
            }
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong RemoveMemberAsync phòng ban {DepartmentId}, User {UserId}: {Message}", departmentId, userId, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Bổ nhiệm hoặc hủy bổ nhiệm Trưởng phòng
    [Authorize(InternalApprovalPermissions.Departments.ManageMembers)]
    public virtual async Task SetManagerAsync(Guid departmentId, Guid? userId)
    {
        try
        {
            var dept = await _departmentRepository.GetAsync(departmentId);
            dept.SetManager(userId);
            await _departmentRepository.UpdateAsync(dept, autoSave: true);

            if (userId.HasValue)
            {
                var member = await _memberRepository.FirstOrDefaultAsync(x => x.DepartmentId == departmentId && x.UserId == userId.Value);
                if (member == null)
                {
                    var newMember = new DepartmentMember(GuidGenerator.Create(), departmentId, userId.Value, "Trưởng phòng");
                    await _memberRepository.InsertAsync(newMember, autoSave: true);
                }
                else if (string.IsNullOrWhiteSpace(member.Position) || member.Position == "Nhân viên")
                {
                    member.Position = "Trưởng phòng";
                    await _memberRepository.UpdateAsync(member, autoSave: true);
                }
            }
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong SetManagerAsync phòng ban {DepartmentId}: {Message}", departmentId, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Lấy danh sách user khả dụng để gán vào phòng ban
    public virtual async Task<List<DepartmentUserLookupDto>> GetAvailableUsersAsync(Guid departmentId)
    {
        try
        {
            var allUsers = await _userRepository.GetListAsync();
            var allMembers = await _memberRepository.GetListAsync();
            var allDepts = await _departmentRepository.GetListAsync();

            var deptDict = allDepts.ToDictionary(x => x.Id, x => x.Name);
            var memberDict = allMembers
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            return allUsers.Select(u =>
            {
                memberDict.TryGetValue(u.Id, out var membership);
                string? curDeptName = null;
                string? curPos = null;

                if (membership != null && deptDict.TryGetValue(membership.DepartmentId, out var dName))
                {
                    curDeptName = dName;
                    curPos = membership.Position;
                }

                return new DepartmentUserLookupDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    FullName = (!string.IsNullOrWhiteSpace(u.Surname) ? u.Surname + " " : "") + u.Name,
                    Email = u.Email,
                    CurrentDepartmentName = curDeptName,
                    CurrentPosition = curPos
                };
            }).OrderBy(x => x.FullName).ToList();
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetAvailableUsersAsync phòng ban {DepartmentId}: {Message}", departmentId, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }
}
