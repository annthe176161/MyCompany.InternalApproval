namespace MyCompany.InternalApproval;

public static class InternalApprovalDomainErrorCodes
{
    public const string OnlyDraftCanBeSubmitted = "InternalApproval:00001";
    public const string OnlyCreatorCanSubmit = "InternalApproval:00002";
    public const string CannotApproveOwnRequest = "InternalApproval:00003";
    public const string OnlySubmittedCanBeApproved = "InternalApproval:00004";
    public const string RejectionReasonRequired = "InternalApproval:00005";
    public const string OnlySubmittedCanBeRejected = "InternalApproval:00006";
    public const string AmountRequiredForType = "InternalApproval:00007";
    public const string OnlyDraftCanBeEdited = "InternalApproval:00008";
    public const string OnlyCreatorCanEdit = "InternalApproval:00009";
    public const string OnlyDraftCanBeDeleted = "InternalApproval:00010";
    public const string OnlyCreatorCanDelete = "InternalApproval:00011";
    public const string MappingError = "InternalApproval:00012";
    public const string InvalidDateRange = "InternalApproval:00013";
    public const string InvalidJoinDate = "InternalApproval:00014";
}
