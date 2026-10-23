using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MyCompany.InternalApproval.Localization;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Validation;

namespace MyCompany.InternalApproval.Middleware;

/// <summary>
/// Chuẩn hóa response về HTTP 200 cho các API nghiệp vụ (/api/app/...) khi gặp lỗi/exception.
/// Giúp Client nhận cùng 1 cấu trúc thống nhất (success, code, message, data).
/// </summary>
public class UnifiedApiResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UnifiedApiResponseMiddleware> _logger;

    public UnifiedApiResponseMiddleware(
        RequestDelegate next,
        ILogger<UnifiedApiResponseMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IStringLocalizer<InternalApprovalResource> localizer)
    {
        // Chỉ can thiệp vào các API nghiệp vụ (/api/app/...)
        // Tránh can thiệp vào các luồng Authentication, OAuth, OpenIddict, Account hay Swagger
        var path = context.Request.Path.Value ?? string.Empty;
        var isApplicationApi = path.StartsWith("/api/app/", StringComparison.OrdinalIgnoreCase);

        if (!isApplicationApi)
        {
            await _next(context);
            return;
        }

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra tại API nghiệp vụ {Path}: {Message}", path, ex.Message);
            await HandleExceptionAsync(context, ex, localizer);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception ex,
        IStringLocalizer<InternalApprovalResource> localizer)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        string errorCode = "InternalApproval:00000";
        string message = "Đã xảy ra sự cố trong quá trình xử lý yêu cầu.";

        if (ex is BusinessException businessEx)
        {
            errorCode = businessEx.Code ?? "InternalApproval:BusinessError";
            message = !string.IsNullOrWhiteSpace(businessEx.Code)
                ? localizer[businessEx.Code].Value
                : businessEx.Message;
        }
        else if (ex is AbpAuthorizationException)
        {
            errorCode = "InternalApproval:403";
            message = "Bạn không có quyền thực hiện thao tác này.";
        }
        else if (ex is EntityNotFoundException notFoundEx)
        {
            errorCode = "InternalApproval:404";
            message = $"Không tìm thấy dữ liệu yêu cầu ({notFoundEx.EntityType?.Name ?? "Bản ghi"}).";
        }
        else if (ex is AbpValidationException valEx)
        {
            errorCode = "InternalApproval:400";
            message = valEx.ValidationErrors.Count > 0
                ? string.Join("; ", System.Linq.Enumerable.Select(valEx.ValidationErrors, e => e.ErrorMessage))
                : "Dữ liệu gửi lên không hợp lệ.";
        }
        else
        {
            // Các lỗi hệ thống khác
            errorCode = "InternalApproval:500";
            message = ex.Message;
        }

        // Ép HTTP Status trả về 200 OK theo đúng chuẩn Envelope Pattern
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";

        // Xóa header _AbpErrorFormat nếu có để tránh Angular theme của ABP tự động bật popup đỏ
        context.Response.Headers.Remove("_AbpErrorFormat");

        var responsePayload = new
        {
            success = false,
            code = errorCode,
            message = message,
            data = (object?)null,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(responsePayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
