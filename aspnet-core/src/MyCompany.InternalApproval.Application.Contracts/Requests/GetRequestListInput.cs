using System;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Requests;

public class GetRequestListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public RequestStatus? Status { get; set; }
    public RequestType? Type { get; set; }
    public Guid? CreatorId { get; set; }
}

