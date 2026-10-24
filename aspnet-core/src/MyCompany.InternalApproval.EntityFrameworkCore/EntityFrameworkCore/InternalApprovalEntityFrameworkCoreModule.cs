using Microsoft.Extensions.DependencyInjection;
using MyCompany.InternalApproval.Departments;
using MyCompany.InternalApproval.Notifications;
using MyCompany.InternalApproval.Requests;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;

namespace MyCompany.InternalApproval.EntityFrameworkCore;

// Cấu hình Module EntityFrameworkCore và đăng ký các Repository của hệ thống
[DependsOn(
    typeof(InternalApprovalDomainModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpSettingManagementEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule),
    typeof(AbpBackgroundJobsEntityFrameworkCoreModule)
)]
public class InternalApprovalEntityFrameworkCoreModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        InternalApprovalEfCoreEntityExtensionMappings.Configure();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<InternalApprovalDbContext>(options =>
        {
            // Đăng ký Repository mặc định và các Custom Repository theo từng module
            options.AddDefaultRepositories(includeAllEntities: true);
            options.AddRepository<Request, EfCoreRequestRepository>();
            options.AddRepository<Department, EfCoreDepartmentRepository>();
            options.AddRepository<ApprovalNotification, EfCoreApprovalNotificationRepository>();
        });

        Configure<AbpDbContextOptions>(options =>
        {
            options.UseSqlServer();
        });
    }
}
