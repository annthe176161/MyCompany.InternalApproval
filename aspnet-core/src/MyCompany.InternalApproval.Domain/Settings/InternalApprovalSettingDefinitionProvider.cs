using Volo.Abp.Settings;

namespace MyCompany.InternalApproval.Settings;

public class InternalApprovalSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(InternalApprovalSettings.MySetting1));
    }
}
