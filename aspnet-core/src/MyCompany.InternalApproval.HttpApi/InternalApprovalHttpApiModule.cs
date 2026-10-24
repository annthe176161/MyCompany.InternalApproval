using Localization.Resources.AbpUi;
using MyCompany.InternalApproval.Localization;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;

namespace MyCompany.InternalApproval;

// Cấu hình module HttpApi cung cấp các điểm cuối REST API
[DependsOn(
    typeof(InternalApprovalApplicationContractsModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpPermissionManagementHttpApiModule)
)]
public class InternalApprovalHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureLocalization();
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<InternalApprovalResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );

            options.Resources
                .Get<AbpUiResource>()
                .AddVirtualJson("/Localization/AbpUi");
        });
    }
}
