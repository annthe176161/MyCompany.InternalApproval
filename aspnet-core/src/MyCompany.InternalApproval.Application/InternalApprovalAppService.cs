using System;
using System.Collections.Generic;
using System.Text;
using MyCompany.InternalApproval.Localization;
using Volo.Abp.Application.Services;

namespace MyCompany.InternalApproval;

/* Inherit your application services from this class.
 */
public abstract class InternalApprovalAppService : ApplicationService
{
    protected InternalApprovalAppService()
    {
        LocalizationResource = typeof(InternalApprovalResource);
    }
}
