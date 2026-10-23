using System;

namespace MyCompany.InternalApproval.Departments;

public class DepartmentMemberDto
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool IsManager { get; set; }
    public DateTime? JoinDate { get; set; }
    public DateTime EffectiveJoinDate { get; set; }
    public string? TenureFormatted { get; set; }
    public DateTime CreationTime { get; set; }
}

public class AddDepartmentMemberDto
{
    public Guid UserId { get; set; }
    public string Position { get; set; } = string.Empty;
    public DateTime? JoinDate { get; set; }
}

public class UpdateDepartmentMemberDto
{
    public string Position { get; set; } = string.Empty;
    public DateTime? JoinDate { get; set; }
}

public class DepartmentUserLookupDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CurrentDepartmentName { get; set; }
    public string? CurrentPosition { get; set; }
}

