using System.Collections.Generic;

namespace MyCompany.InternalApproval.Employees;

// Thống kê đề xuất theo năm của nhân viên: số lượng theo trạng thái, số tiền và tổng ngày nghỉ
public class EmployeeYearlyStatsDto
{
    // Năm thống kê
    public int Year { get; set; }

    // Tổng số đề xuất trong năm
    public int TotalRequests { get; set; }

    // Số lượng đề xuất đã duyệt
    public int ApprovedCount { get; set; }

    // Số lượng đề xuất đang chờ duyệt
    public int SubmittedCount { get; set; }

    // Số lượng đề xuất nháp
    public int DraftCount { get; set; }

    // Số lượng đề xuất bị từ chối
    public int RejectedCount { get; set; }

    // Tổng số tiền đề xuất
    public decimal TotalAmountRequested { get; set; }

    // Tổng số tiền đã được phê duyệt giải ngân
    public decimal TotalAmountDisbursed { get; set; }

    // Tổng số ngày nghỉ phép đã được duyệt trong năm
    public double LeaveDaysTotal { get; set; }

    // Chi tiết thống kê theo từng loại đề xuất
    public List<RequestTypeStatDto> BreakdownByType { get; set; } = new();
}

