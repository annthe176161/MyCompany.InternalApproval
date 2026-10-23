using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using MyCompany.InternalApproval.Requests;
using MyCompany.InternalApproval.Notifications;
using MyCompany.InternalApproval.Departments;

namespace MyCompany.InternalApproval.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class InternalApprovalDbContext :
    AbpDbContext<InternalApprovalDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    //Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    public DbSet<Request> Requests { get; set; }
    public DbSet<RequestComment> RequestComments { get; set; }
    public DbSet<ApprovalNotification> ApprovalNotifications { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<DepartmentMember> DepartmentMembers { get; set; }

    #endregion

    public InternalApprovalDbContext(DbContextOptions<InternalApprovalDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        /* Configure your own tables/entities inside here */

        builder.Entity<Request>(b =>
        {
            b.ToTable(InternalApprovalConsts.DbTablePrefix + "Requests", InternalApprovalConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.RejectionReason).HasMaxLength(1000);
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.Property(x => x.StartDate).IsRequired(false);
            b.Property(x => x.EndDate).IsRequired(false);

            b.HasMany(x => x.Comments)
                .WithOne()
                .HasForeignKey(x => x.RequestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CreatorId);
            b.HasIndex(x => x.CreationTime);
        });

        builder.Entity<RequestComment>(b =>
        {
            b.ToTable(InternalApprovalConsts.DbTablePrefix + "RequestComments", InternalApprovalConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Content).IsRequired().HasMaxLength(2000);
            b.HasIndex(x => x.RequestId);
        });

        builder.Entity<ApprovalNotification>(b =>
        {
            b.ToTable(InternalApprovalConsts.DbTablePrefix + "ApprovalNotifications", InternalApprovalConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Message).IsRequired().HasMaxLength(2000);

            b.HasIndex(x => x.ReceiverId);
            b.HasIndex(x => x.RequestId);
            b.HasIndex(x => x.CreationTime);
        });

        builder.Entity<Department>(b =>
        {
            b.ToTable(InternalApprovalConsts.DbTablePrefix + "Departments", InternalApprovalConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasIndex(x => x.Code).IsUnique();

            b.HasMany(x => x.Members)
                .WithOne()
                .HasForeignKey(x => x.DepartmentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DepartmentMember>(b =>
        {
            b.ToTable(InternalApprovalConsts.DbTablePrefix + "DepartmentMembers", InternalApprovalConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Position).IsRequired().HasMaxLength(128);
            b.Property(x => x.JoinDate).IsRequired(false);

            b.HasIndex(x => new { x.DepartmentId, x.UserId }).IsUnique();
            b.HasIndex(x => x.UserId);
        });
    }
}
