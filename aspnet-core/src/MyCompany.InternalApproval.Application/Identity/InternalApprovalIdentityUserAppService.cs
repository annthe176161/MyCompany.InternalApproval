using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace MyCompany.InternalApproval.Identity;

// Ghi đè IdentityUserAppService để tự động gán vai trò Employee khi admin tạo tài khoản mới mà chưa chọn vai trò
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IIdentityUserAppService), typeof(IdentityUserAppService), typeof(InternalApprovalIdentityUserAppService))]
public class InternalApprovalIdentityUserAppService : IdentityUserAppService
{
    public InternalApprovalIdentityUserAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        IOptions<IdentityOptions> identityOptions,
        IPermissionChecker permissionChecker)
        : base(userManager, userRepository, roleRepository, identityOptions, permissionChecker)
    {
    }

    public override async Task<IdentityUserDto> CreateAsync(IdentityUserCreateDto input)
    {
        // Nếu admin chưa chọn vai trò nào khi tạo mới tài khoản, tự động gán vai trò Employee mặc định
        if (input.RoleNames == null || input.RoleNames.Length == 0)
        {
            input.RoleNames = new[] { "Employee" };
        }

        var result = await base.CreateAsync(input);

        // Đảm bảo người dùng luôn có tối thiểu vai trò Employee trong hệ thống nếu chưa có bất kỳ vai trò nào
        var user = await UserManager.FindByIdAsync(result.Id.ToString());
        if (user != null)
        {
            var currentRoles = await UserManager.GetRolesAsync(user);
            if (currentRoles == null || currentRoles.Count == 0)
            {
                await UserManager.AddToRoleAsync(user, "Employee");
            }
        }

        return result;
    }
}
