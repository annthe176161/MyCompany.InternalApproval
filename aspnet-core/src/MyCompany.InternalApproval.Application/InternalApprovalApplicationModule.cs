using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;

namespace MyCompany.InternalApproval;

// Cấu hình module Application cài đặt các dịch vụ ứng dụng và AutoMapper
[DependsOn(
    typeof(InternalApprovalDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(InternalApprovalApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule)
)]
public class InternalApprovalApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<InternalApprovalApplicationModule>();
        });
    }
}
