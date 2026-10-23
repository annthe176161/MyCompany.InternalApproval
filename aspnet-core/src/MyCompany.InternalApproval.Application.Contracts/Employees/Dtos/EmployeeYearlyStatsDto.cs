using System.Collections.Generic;
using MyCompany.InternalApproval.Requests;

namespace MyCompany.InternalApproval.Employees;

/// <summary>
/// Thống kê đề xuất theo năm của nhân viên: số lượng theo trạng thái, số tiền và tổng ngày nghỉ.
/// </summary>
public class EmployeeYearlyStatsDto
{
    public int Year { get; set; }
    public int TotalRequests { get; set; }
    public int ApprovedCount { get; set; }
    public int SubmittedCount { get; set; }
    public int DraftCount { get; set; }
    public int RejectedCount { get; set; }
    public decimal TotalAmountRequested { get; set; }
    public decimal TotalAmountDisbursed { get; set; }
    public double LeaveDaysTotal { get; set; }
    public List<RequestTypeStatDto> BreakdownByType { get; set; } = new();
}

public class RequestTypeStatDto
{
    public RequestType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}
