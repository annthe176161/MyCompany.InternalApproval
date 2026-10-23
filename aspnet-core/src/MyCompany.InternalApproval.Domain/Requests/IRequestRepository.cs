using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MyCompany.InternalApproval.Requests;

public interface IRequestRepository : IRepository<Request, Guid>
{
    Task<List<Request>> GetListWithDetailsAsync(
        string? filter = null,
        RequestStatus? status = null,
        RequestType? type = null,
        Guid? creatorId = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        string? filter = null,
        RequestStatus? status = null,
        RequestType? type = null,
        Guid? creatorId = null,
        CancellationToken cancellationToken = default);

    Task<Request?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

