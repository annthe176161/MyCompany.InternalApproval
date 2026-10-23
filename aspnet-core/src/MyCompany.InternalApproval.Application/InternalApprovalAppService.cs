using System;
using Microsoft.Extensions.Logging;
using MyCompany.InternalApproval.Localization;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace MyCompany.InternalApproval;

// Base Application Service dùng chung cho toàn bộ dự án
public abstract class InternalApprovalAppService : ApplicationService
{
    protected InternalApprovalAppService()
    {
        LocalizationResource = typeof(InternalApprovalResource);
    }

    // Ánh xạ dữ liệu qua AutoMapper kèm bắt lỗi ngoại lệ
    protected virtual TDestination MapWithExceptionHandling<TSource, TDestination>(TSource source)
    {
        try
        {
            return ObjectMapper.Map<TSource, TDestination>(source);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Lỗi AutoMapper khi chuyển đổi kiểu từ {Source} sang {Destination}: {Message}", typeof(TSource).Name, typeof(TDestination).Name, ex.Message);
            throw new BusinessException(InternalApprovalDomainErrorCodes.MappingError)
                .WithData("Source", typeof(TSource).Name)
                .WithData("Destination", typeof(TDestination).Name);
        }
    }
}
