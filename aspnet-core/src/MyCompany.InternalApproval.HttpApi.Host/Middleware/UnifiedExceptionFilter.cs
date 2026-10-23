using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Mvc.ExceptionHandling;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Http;

namespace MyCompany.InternalApproval.Middleware;

/// <summary>
/// Thay thế AbpExceptionFilter mặc định: với API nghiệp vụ (/api/app/...),
/// mọi exception đều trả về HTTP 200 với cùng 1 form { success, code, message, data, validationErrors }.
/// </summary>
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(AbpExceptionFilter))]
public class UnifiedExceptionFilter : AbpExceptionFilter
{
    protected override async Task HandleAndWrapException(ExceptionContext context)
    {
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/app/", StringComparison.OrdinalIgnoreCase))
        {
            await base.HandleAndWrapException(context);
            return;
        }

        var services = context.HttpContext.RequestServices;
        var logger = services.GetRequiredService<ILogger<UnifiedExceptionFilter>>();
        var converter = services.GetRequiredService<IExceptionToErrorInfoConverter>();

        // Vẫn ghi log đầy đủ StackTrace để dev/leader debug
        logger.LogError(context.Exception, "Lỗi API {Path}: {Message}", path, context.Exception.Message);

        // Dùng converter của ABP để lấy message đã được localize (vi/en)
        var errorInfo = converter.Convert(context.Exception, options =>
        {
            options.SendExceptionsDetailsToClients = false;
            options.SendStackTraceToClients = false;
        });

        await services.GetRequiredService<IExceptionNotifier>()
            .NotifyAsync(new ExceptionNotificationContext(context.Exception));

        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        context.Result = new ObjectResult(new
        {
            success = false,
            code = errorInfo.Code ?? "InternalApproval:00000",
            message = errorInfo.Message,
            details = errorInfo.Details,
            data = (object?)null,
            validationErrors = errorInfo.ValidationErrors
        })
        {
            StatusCode = StatusCodes.Status200OK
        };

        context.ExceptionHandled = true;
    }
}
