using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCompany.InternalApproval.Data;
using Volo.Abp.DependencyInjection;

namespace MyCompany.InternalApproval.EntityFrameworkCore;

public class EntityFrameworkCoreInternalApprovalDbSchemaMigrator
    : IInternalApprovalDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreInternalApprovalDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the InternalApprovalDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<InternalApprovalDbContext>()
            .Database
            .MigrateAsync();
    }
}
