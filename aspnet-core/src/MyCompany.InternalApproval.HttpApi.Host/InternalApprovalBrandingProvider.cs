using Microsoft.Extensions.Localization;
using MyCompany.InternalApproval.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace MyCompany.InternalApproval;

[Dependency(ReplaceServices = true)]
public class InternalApprovalBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<InternalApprovalResource> _localizer;

    public InternalApprovalBrandingProvider(IStringLocalizer<InternalApprovalResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
