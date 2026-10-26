using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace MyCompany.InternalApproval.Identity;

// Ghi đè IdentityRoleAppService để đảm bảo toàn hệ thống chỉ có DUY NHẤT 1 vai trò mặc định
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IIdentityRoleAppService), typeof(IdentityRoleAppService), typeof(InternalApprovalIdentityRoleAppService))]
public class InternalApprovalIdentityRoleAppService : IdentityRoleAppService
{
    public InternalApprovalIdentityRoleAppService(
        IdentityRoleManager roleManager,
        IIdentityRoleRepository roleRepository)
        : base(roleManager, roleRepository)
    {
    }

    // Khi tạo vai trò mới với IsDefault = true, hủy cờ mặc định của tất cả các vai trò khác
    public override async Task<IdentityRoleDto> CreateAsync(IdentityRoleCreateDto input)
    {
        if (input.IsDefault)
        {
            var defaultRoles = await RoleRepository.GetDefaultOnesAsync();
            foreach (var role in defaultRoles)
            {
                role.IsDefault = false;
                await RoleRepository.UpdateAsync(role);
            }
        }

        return await base.CreateAsync(input);
    }

    // Khi cập nhật vai trò: nếu đặt làm mặc định thì chuyển các vai trò khác về thường; không cho phép bỏ mặc định nếu là vai trò mặc định duy nhất
    public override async Task<IdentityRoleDto> UpdateAsync(Guid id, IdentityRoleUpdateDto input)
    {
        var currentRole = await RoleRepository.GetAsync(id);

        if (input.IsDefault)
        {
            var defaultRoles = await RoleRepository.GetDefaultOnesAsync();
            foreach (var role in defaultRoles.Where(r => r.Id != id))
            {
                role.IsDefault = false;
                await RoleRepository.UpdateAsync(role);
            }
        }
        else if (currentRole.IsDefault)
        {
            var defaultRoles = await RoleRepository.GetDefaultOnesAsync();
            var otherDefaultRoles = defaultRoles.Where(r => r.Id != id).ToList();
            if (!otherDefaultRoles.Any())
            {
                throw new UserFriendlyException("Hệ thống luôn cần duy trì duy nhất 1 vai trò mặc định cho nhân viên mới. Để đổi vai trò mặc định, vui lòng chọn 'Mặc định' ở vai trò bạn muốn thay thế.");
            }
        }

        return await base.UpdateAsync(id, input);
    }

    // Không cho phép xóa vai trò đang là mặc định để tránh mất vai trò mặc định của hệ thống
    public override async Task DeleteAsync(Guid id)
    {
        var role = await RoleRepository.GetAsync(id);
        if (role.IsDefault)
        {
            throw new UserFriendlyException("Không thể xóa vai trò đang là mặc định của hệ thống! Vui lòng chỉ định một vai trò khác làm mặc định trước khi xóa vai trò này.");
        }

        await base.DeleteAsync(id);
    }
}
