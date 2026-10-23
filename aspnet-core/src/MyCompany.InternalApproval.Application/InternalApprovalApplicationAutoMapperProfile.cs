using System;
using AutoMapper;
using MyCompany.InternalApproval.Notifications;
using MyCompany.InternalApproval.Requests;

namespace MyCompany.InternalApproval;

public class InternalApprovalApplicationAutoMapperProfile : Profile
{
    public InternalApprovalApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */

        CreateMap<Request, RequestDto>()
            .ForMember(dest => dest.CreatorName, opt => opt.Ignore())
            .ForMember(dest => dest.ApproverName, opt => opt.Ignore());

        CreateMap<RequestComment, RequestCommentDto>()
            .ForMember(dest => dest.CreatorName, opt => opt.Ignore());

        CreateMap<ApprovalNotification, ApprovalNotificationDto>()
            .ForMember(dest => dest.ApproverName, opt => opt.Ignore());
    }
}
