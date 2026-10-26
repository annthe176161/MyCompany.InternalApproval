using System;
using System.Collections.Generic;
using System.Linq;
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
using Volo.Abp.PermissionManagement;
using Volo.Abp.Validation;

namespace MyCompany.InternalApproval.Departments;

// Dịch vụ quản lý cơ cấu phòng ban và phân bổ thành viên
[Authorize]
[RemoteService(IsEnabled = false)]
public class DepartmentAppService : InternalApprovalAppService, IDepartmentAppService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IRepository<DepartmentMember, Guid> _memberRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IPermissionManager _permissionManager;

    public DepartmentAppService(
        IDepartmentRepository departmentRepository,
        IRepository<DepartmentMember, Guid> memberRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IdentityUserManager userManager,
        IPermissionManager permissionManager)
    {
        _departmentRepository = departmentRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _userManager = userManager;
        _permissionManager = permissionManager;
    }

    // Lấy danh sách phòng ban có phân trang và sắp xếp
    public virtual async Task<PagedResultDto<DepartmentDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        try
        {
            var totalCount = await _departmentRepository.GetCountAsync();
            var pagedDepts = await _departmentRepository.GetListWithDetailsAsync(
                input.Sorting,
                input.MaxResultCount,
                input.SkipCount
            );

            var managerIds = pagedDepts
                .Where(x => x.ManagerId.HasValue)
                .Select(x => x.ManagerId!.Value)
                .Distinct()
                .ToList();

            var managers = managerIds.Any()
                ? await _userRepository.GetListAsync(x => managerIds.Contains(x.Id))
                : new List<IdentityUser>();

            var managerDict = managers.ToDictionary(
                x => x.Id,
                x => (!string.IsNullOrWhiteSpace(x.Surname) ? x.Surname + " " : "") + x.Name
            );

            var dtos = MapWithExceptionHandling<List<Department>, List<DepartmentDto>>(pagedDepts)
                .Select((dto, idx) =>
                {
                    var entity = pagedDepts[idx];
                    dto.MemberCount = entity.Members.Count;
                    if (dto.ManagerId.HasValue && managerDict.TryGetValue(dto.ManagerId.Value, out var mgrName))
                    {
                        dto.ManagerName = mgrName;
                    }
                    return dto;
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
            var dept = await _departmentRepository.GetWithDetailsAsync(id);
            if (dept == null)
            {
                throw new EntityNotFoundException(typeof(Department), id);
            }

            var dto = MapWithExceptionHandling<Department, DepartmentDto>(dept);
            dto.MemberCount = dept.Members.Count;

            if (dept.ManagerId.HasValue)
            {
                var manager = await _userRepository.FindAsync(dept.ManagerId.Value);
                if (manager != null)
                {
                    dto.ManagerName = (!string.IsNullOrWhiteSpace(manager.Surname) ? manager.Surname + " " : "") + manager.Name;
                }
            }

            return dto;
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
            var existing = await _departmentRepository.FindByCodeAsync(input.Code.Trim());
            if (existing != null)
            {
                throw new BusinessException(InternalApprovalDomainErrorCodes.DepartmentCodeAlreadyExists)
                    .WithData("Code", input.Code.Trim());
            }

            var dept = new Department(
                GuidGenerator.Create(),
                input.Code.Trim(),
                input.Name.Trim(),
                input.Description?.Trim(),
                input.ManagerId
            );

            // Nếu có chọn trưởng phòng, kiểm tra xem nhân sự này có thuộc phòng ban khác không
            if (input.ManagerId.HasValue)
            {
                var otherMembership = await _memberRepository.FirstOrDefaultAsync(x => x.UserId == input.ManagerId.Value);
                if (otherMembership != null)
                {
                    var otherDept = await _departmentRepository.FindAsync(otherMembership.DepartmentId);
                    var otherDeptName = otherDept?.Name ?? "phòng ban khác";
                    var user = await _userRepository.FindAsync(input.ManagerId.Value);
                    var userName = user != null ? ((!string.IsNullOrWhiteSpace(user.Surname) ? user.Surname + " " : "") + user.Name) : "Nhân sự này";
                    throw new UserFriendlyException($"Nhân sự '{userName}' hiện đang thuộc phòng ban '{otherDeptName}'. Mỗi nhân viên chỉ được thuộc duy nhất 1 phòng ban! Vui lòng gỡ nhân sự khỏi '{otherDeptName}' trước.");
                }

                dept.AddMember(GuidGenerator.Create(), input.ManagerId.Value, "Trưởng phòng");
            }

            await _departmentRepository.InsertAsync(dept, autoSave: true);

            if (input.ManagerId.HasValue)
            {
                await SyncManagerRoleAndPermissionsAsync(null, input.ManagerId, dept.Id);
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
            var oldManagerId = dept.ManagerId;

            var existing = await _departmentRepository.FirstOrDefaultAsync(x => x.Code == input.Code.Trim() && x.Id != id);
            if (existing != null)
            {
                throw new BusinessException(InternalApprovalDomainErrorCodes.DepartmentCodeAlreadyExists)
                    .WithData("Code", input.Code.Trim());
            }

            dept.SetCode(input.Code.Trim())
                .SetName(input.Name.Trim())
                .SetDescription(input.Description?.Trim())
                .SetManager(input.ManagerId);

            await _departmentRepository.UpdateAsync(dept, autoSave: true);

            if (input.ManagerId.HasValue)
            {
                var otherMembership = await _memberRepository.FirstOrDefaultAsync(x => x.UserId == input.ManagerId.Value && x.DepartmentId != id);
                if (otherMembership != null)
                {
                    var otherDept = await _departmentRepository.FindAsync(otherMembership.DepartmentId);
                    var otherDeptName = otherDept?.Name ?? "phòng ban khác";
                    var user = await _userRepository.FindAsync(input.ManagerId.Value);
                    var userName = user != null ? ((!string.IsNullOrWhiteSpace(user.Surname) ? user.Surname + " " : "") + user.Name) : "Nhân sự này";
                    throw new UserFriendlyException($"Nhân sự '{userName}' hiện đang thuộc phòng ban '{otherDeptName}'. Mỗi nhân viên chỉ được thuộc duy nhất 1 phòng ban! Vui lòng gỡ nhân sự khỏi '{otherDeptName}' trước.");
                }

                var existingMember = await _memberRepository.FirstOrDefaultAsync(x => x.DepartmentId == id && x.UserId == input.ManagerId.Value);
                if (existingMember == null)
                {
                    var member = new DepartmentMember(GuidGenerator.Create(), dept.Id, input.ManagerId.Value, "Trưởng phòng");
                    await _memberRepository.InsertAsync(member, autoSave: true);
                }
                else
                {
                    existingMember.SetPosition("Trưởng phòng");
                    await _memberRepository.UpdateAsync(existingMember, autoSave: true);
                }
            }

            if (oldManagerId.HasValue && oldManagerId != input.ManagerId)
            {
                var oldMember = await _memberRepository.FirstOrDefaultAsync(x => x.DepartmentId == id && x.UserId == oldManagerId.Value);
                if (oldMember != null && oldMember.Position == "Trưởng phòng")
                {
                    oldMember.SetPosition("Nhân viên");
                    await _memberRepository.UpdateAsync(oldMember, autoSave: true);
                }
            }

            await SyncManagerRoleAndPermissionsAsync(oldManagerId, input.ManagerId, id);

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
            var dept = await _departmentRepository.GetAsync(id);
            var oldManagerId = dept.ManagerId;

            var members = await _memberRepository.GetListAsync(x => x.DepartmentId == id);
            if (members.Any())
            {
                await _memberRepository.DeleteManyAsync(members, autoSave: true);
            }

            await _departmentRepository.DeleteAsync(id, autoSave: true);

            if (oldManagerId.HasValue)
            {
                await SyncManagerRoleAndPermissionsAsync(oldManagerId, null, id);
            }
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong DeleteAsync phòng ban {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Lấy danh sách thành viên trong phòng ban
    public virtual async Task<List<DepartmentMemberDto>> GetMembersAsync(Guid departmentId)
    {
        try
        {
            var dept = await _departmentRepository.GetWithDetailsAsync(departmentId);
            if (dept == null)
            {
                throw new EntityNotFoundException(typeof(Department), departmentId);
            }

            var members = dept.Members.ToList();
            var userIds = members.Select(x => x.UserId).Distinct().ToList();

            var users = userIds.Any()
                ? await _userRepository.GetListAsync(x => userIds.Contains(x.Id))
                : new List<IdentityUser>();

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

    // Thêm nhân viên vào phòng ban (chặn nếu nhân sự đã thuộc phòng ban khác)
    [Authorize(InternalApprovalPermissions.Departments.ManageMembers)]
    public virtual async Task AddMemberAsync(Guid departmentId, AddDepartmentMemberDto input)
    {
        try
        {
            await _departmentRepository.GetAsync(departmentId);

            // Kiểm tra xem nhân sự này đã thuộc phòng ban nào khác chưa
            var existingOther = await _memberRepository.FirstOrDefaultAsync(x => x.UserId == input.UserId && x.DepartmentId != departmentId);
            if (existingOther != null)
            {
                var otherDept = await _departmentRepository.FindAsync(existingOther.DepartmentId);
                var otherDeptName = otherDept?.Name ?? "phòng ban khác";
                var user = await _userRepository.FindAsync(input.UserId);
                var userName = user != null ? ((!string.IsNullOrWhiteSpace(user.Surname) ? user.Surname + " " : "") + user.Name) : "Nhân sự này";
                throw new UserFriendlyException($"Nhân sự '{userName}' hiện đang thuộc phòng ban '{otherDeptName}'. Mỗi nhân sự chỉ được thuộc duy nhất 1 phòng ban! Vui lòng gỡ nhân sự khỏi '{otherDeptName}' trước khi thêm vào phòng ban mới.");
            }

            var existing = await _memberRepository.FirstOrDefaultAsync(x => x.DepartmentId == departmentId && x.UserId == input.UserId);
            if (existing != null)
            {
                existing.SetPosition(input.Position.Trim());
                if (input.JoinDate.HasValue)
                {
                    existing.SetJoinDate(input.JoinDate.Value);
                }
                await _memberRepository.UpdateAsync(existing, autoSave: true);
                return;
            }

            DateTime? joinDate = input.JoinDate;
            if (!joinDate.HasValue)
            {
                var user = await _userRepository.FindAsync(input.UserId);
                if (user != null)
                {
                    joinDate = TenureCalculator.TryGetJoinDate(user.ExtraProperties);
                }
            }

            var member = new DepartmentMember(GuidGenerator.Create(), departmentId, input.UserId, input.Position.Trim(), joinDate);
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
                throw new BusinessException(InternalApprovalDomainErrorCodes.DepartmentMemberNotFound);
            }

            member.SetPosition(input.Position.Trim());
            if (input.JoinDate.HasValue)
            {
                member.SetJoinDate(input.JoinDate.Value);
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
                await SyncManagerRoleAndPermissionsAsync(userId, null, departmentId);
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
            var oldManagerId = dept.ManagerId;

            dept.SetManager(userId);
            await _departmentRepository.UpdateAsync(dept, autoSave: true);

            if (userId.HasValue)
            {
                // Kiểm tra xem nhân sự này có đang thuộc phòng ban khác không
                var otherMembership = await _memberRepository.FirstOrDefaultAsync(x => x.UserId == userId.Value && x.DepartmentId != departmentId);
                if (otherMembership != null)
                {
                    var otherDept = await _departmentRepository.FindAsync(otherMembership.DepartmentId);
                    var otherDeptName = otherDept?.Name ?? "phòng ban khác";
                    var user = await _userRepository.FindAsync(userId.Value);
                    var userName = user != null ? ((!string.IsNullOrWhiteSpace(user.Surname) ? user.Surname + " " : "") + user.Name) : "Nhân sự này";
                    throw new UserFriendlyException($"Nhân sự '{userName}' hiện đang thuộc phòng ban '{otherDeptName}'. Mỗi nhân sự chỉ được thuộc duy nhất 1 phòng ban! Vui lòng gỡ nhân sự khỏi '{otherDeptName}' trước khi bổ nhiệm làm Trưởng phòng.");
                }

                var member = await _memberRepository.FirstOrDefaultAsync(x => x.DepartmentId == departmentId && x.UserId == userId.Value);
                if (member == null)
                {
                    var newMember = new DepartmentMember(GuidGenerator.Create(), departmentId, userId.Value, "Trưởng phòng");
                    await _memberRepository.InsertAsync(newMember, autoSave: true);
                }
                else
                {
                    member.SetPosition("Trưởng phòng");
                    await _memberRepository.UpdateAsync(member, autoSave: true);
                }
            }

            // Nếu hủy hoặc thay đổi trưởng phòng, chuyển chức danh của người cũ về Nhân viên
            if (oldManagerId.HasValue && oldManagerId != userId)
            {
                var oldMember = await _memberRepository.FirstOrDefaultAsync(x => x.DepartmentId == departmentId && x.UserId == oldManagerId.Value);
                if (oldMember != null && oldMember.Position == "Trưởng phòng")
                {
                    oldMember.SetPosition("Nhân viên");
                    await _memberRepository.UpdateAsync(oldMember, autoSave: true);
                }
            }

            // Đồng bộ vai trò Manager và toàn bộ quyền phê duyệt tương ứng
            await SyncManagerRoleAndPermissionsAsync(oldManagerId, userId, departmentId);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong SetManagerAsync phòng ban {DepartmentId}: {Message}", departmentId, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Đồng bộ vai trò và quyền hạn khi bổ nhiệm hoặc hủy quyền Trưởng phòng
    protected virtual async Task SyncManagerRoleAndPermissionsAsync(Guid? oldManagerId, Guid? newManagerId, Guid departmentId)
    {
        // 1. Thu hồi quyền Trưởng phòng nếu có người quản lý cũ và người đó không còn quản lý phòng ban nào khác
        if (oldManagerId.HasValue && oldManagerId != newManagerId)
        {
            var otherDepts = await _departmentRepository.GetListAsync(d => d.Id != departmentId && d.ManagerId == oldManagerId.Value);
            if (!otherDepts.Any())
            {
                var oldUser = await _userManager.FindByIdAsync(oldManagerId.Value.ToString());
                if (oldUser != null && !string.Equals(oldUser.UserName, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    var oldRoles = await _userManager.GetRolesAsync(oldUser);
                    if (oldRoles.Contains("Manager", StringComparer.OrdinalIgnoreCase))
                    {
                        await _userManager.RemoveFromRoleAsync(oldUser, "Manager");
                    }
                    if (!oldRoles.Contains("Employee", StringComparer.OrdinalIgnoreCase))
                    {
                        await _userManager.AddToRoleAsync(oldUser, "Employee");
                    }

                    await _permissionManager.SetForUserAsync(oldUser.Id, InternalApprovalPermissions.Requests.Approve, false);
                    await _permissionManager.SetForUserAsync(oldUser.Id, InternalApprovalPermissions.Requests.Reject, false);
                    await _permissionManager.SetForUserAsync(oldUser.Id, InternalApprovalPermissions.Employees.Edit, false);

                    // Khôi phục quyền tạo, chỉnh sửa và xóa đề xuất của nhân viên
                    await _permissionManager.SetForUserAsync(oldUser.Id, InternalApprovalPermissions.Requests.Default, true);
                    await _permissionManager.SetForUserAsync(oldUser.Id, InternalApprovalPermissions.Requests.Create, true);
                    await _permissionManager.SetForUserAsync(oldUser.Id, InternalApprovalPermissions.Requests.Edit, true);
                    await _permissionManager.SetForUserAsync(oldUser.Id, InternalApprovalPermissions.Requests.Delete, true);
                }
            }
        }

        // 2. Cấp vai trò Manager và toàn bộ quyền hạn Trưởng phòng cho người được bổ nhiệm mới
        if (newManagerId.HasValue)
        {
            var newUser = await _userManager.FindByIdAsync(newManagerId.Value.ToString());
            if (newUser != null && !string.Equals(newUser.UserName, "admin", StringComparison.OrdinalIgnoreCase))
            {
                var newRoles = await _userManager.GetRolesAsync(newUser);
                if (!newRoles.Contains("Manager", StringComparer.OrdinalIgnoreCase))
                {
                    await _userManager.AddToRoleAsync(newUser, "Manager");
                }
                if (newRoles.Contains("Employee", StringComparer.OrdinalIgnoreCase))
                {
                    await _userManager.RemoveFromRoleAsync(newUser, "Employee");
                }

                // Cấp quyền xem, phê duyệt và từ chối đề xuất cho Trưởng phòng
                await _permissionManager.SetForUserAsync(newUser.Id, InternalApprovalPermissions.Requests.Default, true);
                await _permissionManager.SetForUserAsync(newUser.Id, InternalApprovalPermissions.Requests.Approve, true);
                await _permissionManager.SetForUserAsync(newUser.Id, InternalApprovalPermissions.Requests.Reject, true);
                await _permissionManager.SetForUserAsync(newUser.Id, InternalApprovalPermissions.Departments.Default, true);
                await _permissionManager.SetForUserAsync(newUser.Id, InternalApprovalPermissions.Employees.Default, true);
                await _permissionManager.SetForUserAsync(newUser.Id, InternalApprovalPermissions.Employees.Edit, true);
                await _permissionManager.SetForUserAsync(newUser.Id, InternalApprovalPermissions.Employees.Analytics, true);

                // Loại bỏ quyền tạo, sửa, xóa đề xuất (vì Trưởng phòng chỉ có nhiệm vụ duyệt và từ chối đề xuất)
                await _permissionManager.SetForUserAsync(newUser.Id, InternalApprovalPermissions.Requests.Create, false);
                await _permissionManager.SetForUserAsync(newUser.Id, InternalApprovalPermissions.Requests.Edit, false);
                await _permissionManager.SetForUserAsync(newUser.Id, InternalApprovalPermissions.Requests.Delete, false);
            }
        }
    }

    // Lấy danh sách người dùng khả dụng để gán vào phòng ban
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
