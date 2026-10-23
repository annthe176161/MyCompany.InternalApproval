using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MyCompany.InternalApproval.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class InternalApprovalDbContextFactory : IDesignTimeDbContextFactory<InternalApprovalDbContext>
{
    public InternalApprovalDbContext CreateDbContext(string[] args)
    {
        InternalApprovalEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<InternalApprovalDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new InternalApprovalDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../MyCompany.InternalApproval.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
