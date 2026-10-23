using MyCompany.InternalApproval.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace MyCompany.InternalApproval.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class InternalApprovalController : AbpControllerBase
{
    protected InternalApprovalController()
    {
        LocalizationResource = typeof(InternalApprovalResource);
    }
}
