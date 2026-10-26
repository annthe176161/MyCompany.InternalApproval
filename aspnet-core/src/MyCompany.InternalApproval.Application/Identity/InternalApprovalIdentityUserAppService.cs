using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MyCompany.InternalApproval.Departments;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace MyCompany.InternalApproval.Identity;

// Ghi đè IdentityUserAppService để tự động gán vai trò Employee và đồng bộ ngày vào làm việc (JoinDate)
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IIdentityUserAppService), typeof(IdentityUserAppService), typeof(InternalApprovalIdentityUserAppService))]
public class InternalApprovalIdentityUserAppService : IdentityUserAppService
{
    private readonly IRepository<DepartmentMember, Guid> _memberRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public InternalApprovalIdentityUserAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        IOptions<IdentityOptions> identityOptions,
        IPermissionChecker permissionChecker,
        IRepository<DepartmentMember, Guid> memberRepository,
        IDepartmentRepository departmentRepository)
        : base(userManager, userRepository, roleRepository, identityOptions, permissionChecker)
    {
        _memberRepository = memberRepository;
        _departmentRepository = departmentRepository;
    }

    public override async Task<IdentityUserDto> CreateAsync(IdentityUserCreateDto input)
    {
        // Lấy vai trò mặc định đang kích hoạt trong hệ thống (mặc định là Employee nếu chưa tìm thấy)
        var defaultRole = (await RoleRepository.GetDefaultOnesAsync()).FirstOrDefault();
        var defaultRoleName = defaultRole?.Name ?? "Employee";

        // Nếu admin chưa chọn vai trò nào khi tạo mới tài khoản, tự động gán vai trò mặc định duy nhất của hệ thống
        if (input.RoleNames == null || input.RoleNames.Length == 0)
        {
            input.RoleNames = new[] { defaultRoleName };
        }

        var result = await base.CreateAsync(input);

        // Đảm bảo người dùng luôn có vai trò mặc định trong hệ thống nếu chưa có bất kỳ vai trò nào
        var user = await UserManager.FindByIdAsync(result.Id.ToString());
        if (user != null)
        {
            var currentRoles = await UserManager.GetRolesAsync(user);
            if (currentRoles == null || currentRoles.Count == 0)
            {
                await UserManager.AddToRoleAsync(user, defaultRoleName);
            }
        }

        await TrySyncJoinDateAsync(result.Id, input.ExtraProperties);

        return result;
    }

    public override async Task<IdentityUserDto> UpdateAsync(Guid id, IdentityUserUpdateDto input)
    {
        var result = await base.UpdateAsync(id, input);
        await TrySyncJoinDateAsync(id, input.ExtraProperties);
        return result;
    }

    private async Task TrySyncJoinDateAsync(Guid userId, Volo.Abp.Data.ExtraPropertyDictionary? extraProperties)
    {
        if (extraProperties == null) return;

        object? val = null;
        if (extraProperties.TryGetValue("JoinDate", out var v1)) val = v1;
        else if (extraProperties.TryGetValue("joinDate", out var v2)) val = v2;

        if (val != null)
        {
            DateTime joinDate;
            if (val is DateTime dt)
            {
                joinDate = dt;
            }
            else if (DateTime.TryParse(val.ToString(), out var parsed))
            {
                joinDate = parsed;
            }
            else
            {
                return;
            }

            var member = await _memberRepository.FirstOrDefaultAsync(x => x.UserId == userId);
            if (member != null)
            {
                member.SetJoinDate(joinDate.Date);
                await _memberRepository.UpdateAsync(member, autoSave: true);
            }
            else
            {
                var firstDept = await _departmentRepository.FirstOrDefaultAsync();
                if (firstDept != null)
                {
                    var newMember = new DepartmentMember(
                        GuidGenerator.Create(),
                        firstDept.Id,
                        userId,
                        "Nhân viên",
                        joinDate.Date
                    );
                    await _memberRepository.InsertAsync(newMember, autoSave: true);
                }
            }
        }
    }
}

