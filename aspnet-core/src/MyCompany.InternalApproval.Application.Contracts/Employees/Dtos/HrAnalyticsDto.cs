using System;
using System.Collections.Generic;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// DTO chứa toàn bộ dữ liệu phân tích và thống kê tổng thể nhân sự.
/// </summary>
public class HrAnalyticsDto
{
    public int Year { get; set; }
    public CompanyHrSummaryDto CompanySummary { get; set; } = new();
    public List<DepartmentHeadcountDto> HeadcountByDepartment { get; set; } = new();
    public List<DepartmentTenureDto> TenureByDepartment { get; set; } = new();
    public List<DepartmentRequestFrequencyDto> RequestFrequencyByDepartment { get; set; } = new();
    public List<TopRequesterDto> TopRequesters { get; set; } = new();
}

public class CompanyHrSummaryDto
{
    public int TotalEmployees { get; set; }
    public int TotalDepartments { get; set; }
    public double CompanyAverageTenureMonths { get; set; }
    public string CompanyAverageTenureFormatted { get; set; } = string.Empty;
    public int TotalRequestsThisYear { get; set; }
    public decimal TotalDisbursedAmountThisYear { get; set; }
}

public class DepartmentHeadcountDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int Headcount { get; set; }
    public double Percentage { get; set; }
}

public class DepartmentTenureDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public double AverageTenureMonths { get; set; }
    public string AverageTenureFormatted { get; set; } = string.Empty;
    public int MinTenureMonths { get; set; }
    public int MaxTenureMonths { get; set; }
}

public class DepartmentRequestFrequencyDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public double RequestsPerEmployee { get; set; }
}

public class TopRequesterDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int RequestCount { get; set; }
}
