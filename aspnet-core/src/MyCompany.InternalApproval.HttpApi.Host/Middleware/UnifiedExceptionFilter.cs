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

namespace MyCompany.InternalApproval.Middleware;

// Bộ lọc ngoại lệ chuẩn hóa phản hồi lỗi API nghiệp vụ (/api/app/...) về định dạng thống nhất
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

        // Ghi log đầy đủ StackTrace phục vụ theo dõi và gỡ lỗi
        logger.LogError(context.Exception, "Lỗi API {Path}: {Message}", path, context.Exception.Message);

        // Chuyển đổi thông tin lỗi đã được bản địa hóa (vi/en)
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
