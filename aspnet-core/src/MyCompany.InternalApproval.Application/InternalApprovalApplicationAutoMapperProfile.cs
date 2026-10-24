using AutoMapper;
using MyCompany.InternalApproval.Departments;
using MyCompany.InternalApproval.Notifications;
using MyCompany.InternalApproval.Requests;

namespace MyCompany.InternalApproval;

// Cấu hình ánh xạ đối tượng AutoMapper cho toàn bộ tầng Application
public class InternalApprovalApplicationAutoMapperProfile : Profile
{
    public InternalApprovalApplicationAutoMapperProfile()
    {
        // Ánh xạ thực thể Đề xuất và Bình luận
        CreateMap<Request, RequestDto>()
            .ForMember(dest => dest.CreatorName, opt => opt.Ignore())
            .ForMember(dest => dest.ApproverName, opt => opt.Ignore())
            .ForMember(dest => dest.DepartmentName, opt => opt.Ignore())
            .ForMember(dest => dest.DepartmentCode, opt => opt.Ignore());

        CreateMap<RequestComment, RequestCommentDto>()
            .ForMember(dest => dest.CreatorName, opt => opt.Ignore());

        // Ánh xạ thực thể Thông báo phê duyệt
        CreateMap<ApprovalNotification, ApprovalNotificationDto>()
            .ForMember(dest => dest.ApproverName, opt => opt.Ignore());

        // Ánh xạ thực thể Phòng ban
        CreateMap<Department, DepartmentDto>()
            .ForMember(dest => dest.ManagerName, opt => opt.Ignore())
            .ForMember(dest => dest.MemberCount, opt => opt.Ignore());
    }
}
