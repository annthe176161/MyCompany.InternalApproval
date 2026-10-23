namespace MyCompany.InternalApproval.Permissions;

public static class InternalApprovalPermissions
{
    public const string GroupName = "InternalApproval";

    public static class Requests
    {
        public const string Default = "Requests.Default";
        public const string Create = "Requests.Create";
        public const string Edit = "Requests.Edit";
        public const string Delete = "Requests.Delete";
        public const string Approve = "Requests.Approve";
        public const string Reject = "Requests.Reject";
    }

    public static class Departments
    {
        public const string Default = "Departments.Default";
        public const string Create = "Departments.Create";
        public const string Edit = "Departments.Edit";
        public const string Delete = "Departments.Delete";
        public const string ManageMembers = "Departments.ManageMembers";
    }

    public static class Employees
    {
        public const string Default = "Employees.Default";
        public const string ViewAll = "Employees.ViewAll";
        public const string Edit = "Employees.Edit";
        public const string Analytics = "Employees.Analytics";
    }
}
